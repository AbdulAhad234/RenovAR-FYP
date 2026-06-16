#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { query, pool } = require("../src/config/database");
const {
  generateS3Key,
  uploadToS3,
  buildS3Url,
} = require("../src/utils/s3Helper");
const { normalizeRole } = require("./material-map-utils");
const { getWallTextureDescription } = require("./product-copy");

const DEFAULT_MANIFEST_PATH = path.resolve(
  __dirname,
  "../../3D_Models_Dataset/curated-manifest.json",
);

const IMAGE_MIME_TYPES = {
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".webp": "image/webp",
};

const parseArgs = (argv) => {
  const args = {
    manifest: DEFAULT_MANIFEST_PATH,
    dryRun: false,
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg === "--manifest") {
      args.manifest = path.resolve(process.cwd(), argv[index + 1]);
      index += 1;
    } else if (arg === "--dry-run") {
      args.dryRun = true;
    }
  }

  return args;
};

const getMimeType = (filePath) =>
  IMAGE_MIME_TYPES[path.extname(filePath).toLowerCase()] || null;

const loadManifest = (manifestPath) => {
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Manifest not found: ${manifestPath}`);
  }

  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  if (!Array.isArray(manifest.wallTextures)) {
    throw new Error("Manifest must contain wallTextures array");
  }

  return manifest;
};

const resolveDatasetPath = (datasetRoot, relativePath) =>
  path.resolve(datasetRoot, relativePath);

const findWallTextureProduct = async (entry) => {
  const result = await query(
    `
      SELECT p.id, p.name
      FROM products p
      INNER JOIN categories c ON c.id = p.category_id
      WHERE c.slug = 'wall-textures'
        AND LOWER(p.name) = LOWER($1)
      LIMIT 1
    `,
    [entry.name],
  );

  return result.rows[0] || null;
};

const getExistingMapRoles = async (productId) => {
  const result = await query(
    `
      SELECT DISTINCT metadata_json->>'mapRole' AS "mapRole"
      FROM product_assets
      WHERE product_id = $1
        AND asset_type = 'texture'
        AND metadata_json->>'mapRole' IS NOT NULL
    `,
    [productId],
  );

  return new Set(result.rows.map((row) => normalizeRole(row.mapRole)));
};

const createAssetRecord = async (payload) => {
  const result = await query(
    `
      INSERT INTO product_assets (
        product_id,
        asset_type,
        s3_key,
        s3_url,
        file_name,
        file_size,
        mime_type,
        metadata_json
      )
      VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
      RETURNING id
    `,
    [
      payload.productId,
      payload.assetType,
      payload.s3Key,
      payload.s3Url,
      payload.fileName,
      payload.fileSize,
      payload.mimeType,
      payload.metadata || null,
    ],
  );

  return result.rows[0];
};

const uploadTextureMap = async ({ productId, absolutePath, metadata }) => {
  const buffer = fs.readFileSync(absolutePath);
  const fileName = path.basename(absolutePath);
  const mimeType = getMimeType(absolutePath);

  if (!mimeType) {
    throw new Error(`Unsupported image file type: ${absolutePath}`);
  }

  const s3Key = generateS3Key(String(productId), "texture", fileName);
  await uploadToS3(buffer, s3Key, mimeType);

  return createAssetRecord({
    productId,
    assetType: "texture",
    s3Key,
    s3Url: buildS3Url(s3Key),
    fileName,
    fileSize: buffer.length,
    mimeType,
    metadata: {
      ...metadata,
      format: path.extname(fileName).replace(".", "").toLowerCase(),
      originalFileName: fileName,
    },
  });
};

const main = async () => {
  const options = parseArgs(process.argv.slice(2));
  const manifest = loadManifest(options.manifest);
  const datasetRoot = path.dirname(options.manifest);
  const summary = {
    processed: 0,
    missingProducts: 0,
    uploaded: 0,
    skippedExistingRoles: 0,
    failed: 0,
  };

  for (const entry of manifest.wallTextures) {
    try {
      summary.processed += 1;
      const product = await findWallTextureProduct(entry);

      if (!product) {
        console.warn(`[MISS] No wall texture product found for ${entry.name}`);
        summary.missingProducts += 1;
        continue;
      }

      await query(
        `
          UPDATE products
          SET description = $2,
              texture_target = 'wall'
          WHERE id = $1
        `,
        [product.id, getWallTextureDescription(entry)],
      );

      const existingRoles = await getExistingMapRoles(product.id);
      const materialMaps = Object.entries(entry.materialMaps || {});

      for (const [rawRole, relativePath] of materialMaps) {
        const role = normalizeRole(rawRole);

        if (existingRoles.has(role)) {
          summary.skippedExistingRoles += 1;
          continue;
        }

        const absolutePath = resolveDatasetPath(datasetRoot, relativePath);
        if (!fs.existsSync(absolutePath)) {
          throw new Error(`File not found: ${absolutePath}`);
        }

        if (options.dryRun) {
          console.log(
            `[DRY-RUN] Would upload ${role} for ${entry.name}: ${relativePath}`,
          );
          continue;
        }

        await uploadTextureMap({
          productId: product.id,
          absolutePath,
          metadata: {
            importSource: "curated-dataset-manifest",
            manifestKey: entry.importKey,
            datasetPath: relativePath,
            sourceCategory: "Wall Textures",
            brandName: entry.brandName || null,
            tags: entry.tags || ["wall-texture"],
            textureTarget: "wall",
            mapRole: role,
          },
        });
        summary.uploaded += 1;
        existingRoles.add(role);
      }
    } catch (error) {
      summary.failed += 1;
      console.error(`[FAILED] ${entry.name}: ${error.message}`);
    }
  }

  console.log("\nWall texture material map sync summary:");
  console.log(JSON.stringify(summary, null, 2));
};

main()
  .catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
