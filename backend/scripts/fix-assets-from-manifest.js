#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { query, pool } = require("../src/config/database");
const {
  generateS3Key,
  uploadToS3,
  deleteFromS3,
  buildS3Url,
} = require("../src/utils/s3Helper");

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

const MODEL_MIME_TYPES = {
  ".glb": "application/octet-stream",
  ".fbx": "application/octet-stream",
};

const getMimeType = (filePath, assetType) => {
  const extension = path.extname(filePath).toLowerCase();
  if (assetType === "model") return MODEL_MIME_TYPES[extension] || null;
  return IMAGE_MIME_TYPES[extension] || null;
};

const parseArgs = (argv) => {
  const opts = {
    manifest: DEFAULT_MANIFEST_PATH,
    dryRun: false,
    deleteOld: true,
    planOnly: false,
    assetTypes: new Set(["model", "thumbnail"]),
  };

  for (let i = 0; i < argv.length; i += 1) {
    const a = argv[i];
    if (a === "--manifest") {
      opts.manifest = path.resolve(process.cwd(), argv[i + 1]);
      i += 1;
    } else if (a === "--dry-run") {
      opts.dryRun = true;
    } else if (a === "--keep-old") {
      opts.deleteOld = false;
    } else if (a === "--plan-only") {
      opts.planOnly = true;
    } else if (a === "--thumbnails-only") {
      opts.assetTypes = new Set(["thumbnail"]);
    } else if (a === "--models-only") {
      opts.assetTypes = new Set(["model"]);
    }
  }

  return opts;
};

const loadManifest = (manifestPath) => {
  if (!fs.existsSync(manifestPath))
    throw new Error(`Manifest not found: ${manifestPath}`);
  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  if (!Array.isArray(manifest.products))
    throw new Error("Manifest must contain products array");
  return manifest;
};

const findFileByModelBasename = (datasetRoot, modelPath, searchFolder) => {
  const absoluteModelPath = path.resolve(datasetRoot, modelPath || "");
  const modelBase = path
    .basename(absoluteModelPath, path.extname(absoluteModelPath))
    .toLowerCase();
  const searchRoot = path.join(datasetRoot, searchFolder);
  const candidates = [];

  const walk = (dir) => {
    if (!fs.existsSync(dir)) return;
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const absolute = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        walk(absolute);
      } else if (entry.isFile()) {
        const nameOnly = path
          .basename(absolute, path.extname(absolute))
          .toLowerCase();
        if (nameOnly === modelBase) candidates.push(absolute);
      }
    }
  };

  walk(searchRoot);
  return candidates;
};

const resolveAssetPath = (datasetRoot, entry, relativePath, assetType) => {
  let absolutePath = path.resolve(datasetRoot, relativePath || "");
  if (fs.existsSync(absolutePath)) return absolutePath;

  const searchFolder = assetType === "model" ? "3D models" : "Images";
  const candidates = findFileByModelBasename(
    datasetRoot,
    entry.modelPath,
    searchFolder,
  );

  if (candidates.length === 0) {
    throw new Error(
      `Local ${assetType} not found for ${entry.name}; tried ${absolutePath}`,
    );
  }

  if (candidates.length > 1) {
    console.warn(
      `[WARN] Multiple ${assetType} candidates found for ${entry.name}; using first: ${candidates[0]}`,
    );
  }

  absolutePath = candidates[0];
  console.log(`[INFO] Using discovered ${assetType} for ${entry.name}: ${absolutePath}`);
  return absolutePath;
};

const toDatasetRelativePath = (datasetRoot, absolutePath) =>
  path.relative(datasetRoot, absolutePath).split(path.sep).join("/");

const findVendorByEmail = async (email) => {
  const result = await query(
    `SELECT id, email, role FROM users WHERE LOWER(email) = LOWER($1) LIMIT 1`,
    [email],
  );
  return result.rows[0] || null;
};

const findProductByManifestKey = async (manifestKey, vendorId) => {
  const result = await query(
    `SELECT p.id, p.name
     FROM products p
     JOIN product_assets pa ON pa.product_id = p.id
     WHERE pa.metadata_json->>'manifestKey' = $1
       AND p.vendor_id = $2
     LIMIT 1`,
    [manifestKey, vendorId],
  );

  return result.rows[0] || null;
};

const findProductByVendorName = async (vendorId, name) => {
  const result = await query(
    `SELECT id, name FROM products WHERE vendor_id = $1 AND LOWER(name) = LOWER($2) LIMIT 1`,
    [vendorId, name],
  );
  return result.rows[0] || null;
};

const findExistingAssets = async (productId, assetType, manifestKey, datasetPath) => {
  const result = await query(
    `SELECT id, s3_key, s3_url, file_name, metadata_json
     FROM product_assets
     WHERE product_id = $1
       AND asset_type = $2
       AND (metadata_json->>'manifestKey' = $3 OR metadata_json->>'datasetPath' = $4)
    `,
    [productId, assetType, manifestKey, datasetPath],
  );

  return result.rows || [];
};

const createAssetRecord = async (payload) => {
  const result = await query(
    `INSERT INTO product_assets (product_id, asset_type, s3_key, s3_url, file_name, file_size, mime_type, metadata_json)
     VALUES ($1,$2,$3,$4,$5,$6,$7,$8) RETURNING id`,
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

const run = async () => {
  const options = parseArgs(process.argv.slice(2));
  const manifest = loadManifest(options.manifest);
  const datasetRoot = path.dirname(options.manifest);

  if (!options.planOnly) {
    if (!manifest.defaultVendorEmail)
      throw new Error("Manifest must define defaultVendorEmail");

    const vendor = await findVendorByEmail(manifest.defaultVendorEmail);
    if (!vendor)
      throw new Error(`Vendor not found: ${manifest.defaultVendorEmail}`);
    // attach vendor to options for downstream DB lookups
    options.vendor = vendor;
  }

  const plan = [];

  const summary = { processed: 0, replaced: 0, skipped: 0, failed: 0 };

  for (const entry of manifest.products) {
    try {
      summary.processed += 1;
      const manifestKey =
        entry.importKey ||
        (entry.name && entry.name.toLowerCase().replace(/[^a-z0-9]+/g, "-")) ||
        null;
      const resolvedModelPath = resolveAssetPath(
        datasetRoot,
        entry,
        entry.modelPath,
        "model",
      );
      const resolvedImagePath = resolveAssetPath(
        datasetRoot,
        entry,
        entry.primaryImagePath,
        "thumbnail",
      );
      const resolvedModelDatasetPath = toDatasetRelativePath(
        datasetRoot,
        resolvedModelPath,
      );
      const resolvedImageDatasetPath = toDatasetRelativePath(
        datasetRoot,
        resolvedImagePath,
      );

      const assetSpecs = [
        {
          assetType: "model",
          absolutePath: resolvedModelPath,
          manifestDatasetPath: entry.modelPath,
          resolvedDatasetPath: resolvedModelDatasetPath,
          metadata: { role: "ar-model" },
        },
        {
          assetType: "thumbnail",
          absolutePath: resolvedImagePath,
          manifestDatasetPath: entry.primaryImagePath,
          resolvedDatasetPath: resolvedImageDatasetPath,
          metadata: { role: "primary" },
        },
      ].filter((spec) => options.assetTypes.has(spec.assetType));

      for (const spec of assetSpecs) {
        spec.mimeType = getMimeType(spec.absolutePath, spec.assetType);
        if (!spec.mimeType) {
          throw new Error(
            `Unsupported ${spec.assetType} file type: ${spec.absolutePath}`,
          );
        }
      }

      // Plan-only: record discovered mapping and skip DB/S3 operations
      if (options.planOnly) {
        plan.push({
          importKey: entry.importKey || manifestKey,
          name: entry.name,
          modelPath: entry.modelPath,
          chosenModelPath: resolvedModelDatasetPath,
          primaryImagePath: entry.primaryImagePath,
          chosenImagePath: resolvedImageDatasetPath,
          assets: assetSpecs.map((spec) => ({
            assetType: spec.assetType,
            chosenPath: spec.resolvedDatasetPath,
            mimeType: spec.mimeType,
          })),
        });
        continue;
      }

      const vendor = options.vendor;
      const product =
        (await findProductByManifestKey(manifestKey, vendor.id)) ||
        (await findProductByVendorName(vendor.id, entry.name));
      if (!product) {
        console.error(
          `[SKIP] Product not found in DB for ${entry.name} (manifestKey=${manifestKey})`,
        );
        summary.skipped += 1;
        continue;
      }

      const existingAssetsByType = new Map();
      for (const spec of assetSpecs) {
        const existingAssets = await findExistingAssets(
          product.id,
          spec.assetType,
          manifestKey,
          spec.manifestDatasetPath,
        );
        existingAssetsByType.set(spec.assetType, existingAssets);
      }

      if (options.dryRun) {
        const planText = assetSpecs
          .map((spec) => {
            const count = existingAssetsByType.get(spec.assetType).length;
            return `${count} ${spec.assetType} asset(s) with ${spec.resolvedDatasetPath}`;
          })
          .join("; ");
        console.log(`[DRY] Product #${product.id} (${entry.name}): ${planText}`);
        continue;
      }

      for (const spec of assetSpecs) {
        const existingAssets = existingAssetsByType.get(spec.assetType);

        if (options.deleteOld && existingAssets.length > 0) {
          for (const old of existingAssets) {
            try {
              await deleteFromS3(old.s3_key);
              await query(`DELETE FROM product_assets WHERE id = $1`, [old.id]);
              console.log(`[DELETED] old asset ${old.file_name} (${old.s3_key})`);
            } catch (err) {
              console.error(
                `[WARN] Failed to delete old asset id=${old.id}: ${err.message}`,
              );
            }
          }
        }

        const buffer = fs.readFileSync(spec.absolutePath);
        const fileName = path.basename(spec.absolutePath);
        const s3Key = generateS3Key(String(product.id), spec.assetType, fileName);
        await uploadToS3(buffer, s3Key, spec.mimeType);

        const metadata = {
          importSource: "curated-dataset-manifest",
          manifestKey,
          datasetPath: spec.resolvedDatasetPath,
          manifestDatasetPath: spec.manifestDatasetPath,
          sourceCategory: entry.sourceCategory || entry.category || null,
          tags: entry.tags || [],
          originalModelPath: entry.sourceModelPath || null,
          originalPrimaryImagePath: entry.sourcePrimaryImagePath || null,
          curationConfidence: entry.confidence || null,
          format: path.extname(fileName).replace(".", "").toLowerCase(),
          originalFileName: fileName,
          ...spec.metadata,
        };

        await createAssetRecord({
          productId: product.id,
          assetType: spec.assetType,
          s3Key,
          s3Url: buildS3Url(s3Key),
          fileName,
          fileSize: buffer.length,
          mimeType: spec.mimeType,
          metadata,
        });

        console.log(
          `[REPLACED] Product #${product.id} (${entry.name}) ${spec.assetType} -> ${spec.resolvedDatasetPath}`,
        );
        summary.replaced += 1;
      }
    } catch (error) {
      console.error(`[FAILED] ${entry.name}: ${error.message}`);
      summary.failed += 1;
    }
  }

  console.log("\nSummary:", summary);
  if (options.planOnly) {
    const outPath = path.join(datasetRoot, "rename-plan.json");
    fs.writeFileSync(
      outPath,
      JSON.stringify(
        { generatedAt: new Date().toISOString(), items: plan },
        null,
        2,
      ) + "\n",
      "utf8",
    );
    console.log(`[PLAN] Wrote plan to ${outPath}`);
  }
};

run()
  .catch((err) => {
    console.error(err.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
