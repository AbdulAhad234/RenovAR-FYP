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
  const args = { manifest: DEFAULT_MANIFEST_PATH, dryRun: false };
  for (let i = 0; i < argv.length; i += 1) {
    const a = argv[i];
    if (a === "--manifest") {
      args.manifest = path.resolve(process.cwd(), argv[i + 1]);
      i += 1;
    } else if (a === "--dry-run") {
      args.dryRun = true;
    }
  }
  return args;
};

const slugify = (value) => {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/&/g, "and")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
};

const getMimeType = (filePath) => {
  const ext = path.extname(filePath).toLowerCase();
  return IMAGE_MIME_TYPES[ext] || null;
};

const loadManifest = (manifestPath) => {
  if (!fs.existsSync(manifestPath))
    throw new Error(`Manifest not found: ${manifestPath}`);
  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  if (!Array.isArray(manifest.wallTextures))
    throw new Error("Manifest must contain wallTextures array");
  return manifest;
};

const resolveDatasetPath = (datasetRoot, relativePath) =>
  path.resolve(datasetRoot, relativePath);

const ensureFileExists = (absolutePath) => {
  if (!fs.existsSync(absolutePath))
    throw new Error(`File not found: ${absolutePath}`);
};

const findVendorByEmail = async (email) => {
  const res = await query(
    `
      SELECT
        u.id,
        u.email,
        u.role,
        u.display_name AS "displayName",
        vp.company_name AS "companyName"
      FROM users u
      LEFT JOIN vendor_profiles vp ON vp.user_id = u.id
      WHERE LOWER(u.email)=LOWER($1)
      LIMIT 1
    `,
    [email],
  );
  return res.rows[0] || null;
};

const findCategoryBySlug = async (slug) => {
  const res = await query(
    `SELECT id, name, slug FROM categories WHERE slug = $1 LIMIT 1`,
    [slug],
  );
  return res.rows[0] || null;
};

const resolveVendorSpecs = (manifest) => {
  if (
    Array.isArray(manifest.datasetVendors) &&
    manifest.datasetVendors.length > 0
  ) {
    return manifest.datasetVendors;
  }

  return [
    {
      email: manifest.defaultVendorEmail,
      brandName: manifest.defaultBrandName,
    },
  ].filter((vendor) => vendor.email);
};

const loadImportVendors = async (manifest) => {
  const vendorSpecs = resolveVendorSpecs(manifest);
  if (vendorSpecs.length === 0) {
    throw new Error("Manifest must define datasetVendors or defaultVendorEmail");
  }

  const vendors = [];
  for (const vendorSpec of vendorSpecs) {
    const vendor = await findVendorByEmail(vendorSpec.email);
    if (!vendor) {
      throw new Error(
        `Vendor user not found for email: ${vendorSpec.email}. Run npm run seed:dataset-vendors first.`,
      );
    }

    if (vendor.role !== "vendor") {
      throw new Error(
        `User ${vendorSpec.email} must have role "vendor" before import`,
      );
    }

    vendors.push({
      ...vendor,
      brandName:
        vendorSpec.brandName ||
        vendorSpec.companyName ||
        vendor.companyName ||
        vendor.displayName ||
        null,
    });
  }

  return vendors;
};

const pickVendorForEntry = (entry, vendors, index) => {
  if (entry.vendorEmail) {
    const vendor = vendors.find(
      (candidate) =>
        candidate.email.toLowerCase() === entry.vendorEmail.toLowerCase(),
    );
    if (!vendor) {
      throw new Error(
        `Vendor ${entry.vendorEmail} is not listed in manifest.datasetVendors`,
      );
    }
    return vendor;
  }

  return vendors[index % vendors.length];
};

const createCategory = async ({ name, slug, description = null }) => {
  const res = await query(
    `INSERT INTO categories (name, slug, description) VALUES ($1,$2,$3) RETURNING id, name, slug`,
    [name, slug, description],
  );
  return res.rows[0];
};

const findExistingProduct = async (vendorId, productName, categoryId) => {
  const res = await query(
    `SELECT id, name FROM products WHERE vendor_id = $1 AND LOWER(name) = LOWER($2) AND category_id = $3 LIMIT 1`,
    [vendorId, productName, categoryId],
  );
  return res.rows[0] || null;
};

const createProductRecord = async (payload) => {
  const res = await query(
    `INSERT INTO products (vendor_id, category_id, name, brand_name, description, price, dimensions_json, status) VALUES ($1,$2,$3,$4,$5,$6,$7,'draft') RETURNING id, name`,
    [
      payload.vendorId,
      payload.categoryId,
      payload.name,
      payload.brandName || null,
      payload.description || null,
      payload.price || 0,
      payload.dimensions || null,
    ],
  );
  return res.rows[0];
};

const createAssetRecord = async (payload) => {
  const res = await query(
    `INSERT INTO product_assets (product_id, asset_type, s3_key, s3_url, file_name, file_size, mime_type, metadata_json) VALUES ($1,$2,$3,$4,$5,$6,$7,$8) RETURNING id`,
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
  return res.rows[0];
};

const uploadAssetFile = async ({
  productId,
  assetType,
  absolutePath,
  metadata,
}) => {
  const buffer = fs.readFileSync(absolutePath);
  const fileName = path.basename(absolutePath);
  const mimeType = getMimeType(absolutePath);
  if (!mimeType)
    throw new Error(`Unsupported image file type: ${absolutePath}`);
  const s3Key = generateS3Key(String(productId), assetType, fileName);
  await uploadToS3(buffer, s3Key, mimeType);
  return {
    assetType,
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
  };
};

const main = async () => {
  const options = parseArgs(process.argv.slice(2));
  const manifest = loadManifest(options.manifest);
  const datasetRoot = path.dirname(options.manifest);
  const vendors = await loadImportVendors(manifest);

  // Ensure category exists: Wall Textures
  const CAT_SLUG = "wall-textures";
  let category = await findCategoryBySlug(CAT_SLUG);
  if (!category) {
    if (options.dryRun) {
      console.log(
        `[DRY-RUN] Would create category 'Wall Textures' (slug: ${CAT_SLUG})`,
      );
    } else {
      category = await createCategory({
        name: "Wall Textures",
        slug: CAT_SLUG,
        description: "Wall texture maps for AR wall surfaces",
      });
      console.log(`[CREATED] category ${category.name} (id=${category.id})`);
    }
  }

  const summary = {
    processed: 0,
    created: 0,
    skipped: 0,
    uploaded: 0,
    failed: 0,
    byVendor: {},
  };

  for (const [index, entry] of manifest.wallTextures.entries()) {
    try {
      summary.processed += 1;
      const vendor = pickVendorForEntry(entry, vendors, index);
      const brandName =
        entry.brandName ||
        vendor.brandName ||
        manifest.defaultBrandName ||
        vendor.companyName ||
        vendor.displayName ||
        null;
      const name =
        entry.name || entry.textureFolderPath || "unnamed-wall-texture";
      const importKey = entry.importKey || slugify(name);
      const textureAbs = resolveDatasetPath(datasetRoot, entry.textureMapPath);
      const thumbnailAbs = resolveDatasetPath(datasetRoot, entry.thumbnailPath);

      ensureFileExists(textureAbs);
      ensureFileExists(thumbnailAbs);

      const existingProduct = await findExistingProduct(
        vendor.id,
        name,
        category ? category.id : null,
      );
      if (existingProduct) {
        console.log(
          `[SKIP] Product already exists: ${name} (id=${existingProduct.id})`,
        );
        summary.skipped += 1;
        continue;
      }

      const productPreview = {
        name,
        brandName,
        vendorEmail: vendor.email,
        categorySlug: CAT_SLUG,
      };
      summary.byVendor[vendor.email] = (summary.byVendor[vendor.email] || 0) + 1;
      if (options.dryRun) {
        console.log(
          `[DRY-RUN] Would create product: ${JSON.stringify(productPreview)}`,
        );
        summary.created += 1;
        continue;
      }

      const product = await createProductRecord({
        vendorId: vendor.id,
        categoryId: category.id,
        name,
        brandName,
        description: entry.fileUse || null,
        price: entry.price || 0,
      });
      summary.created += 1;

      const baseMetadata = {
        importSource: "curated-dataset-manifest",
        manifestKey: importKey,
        datasetPath: entry.textureMapPath,
        sourceCategory: "Wall Textures",
        brandName,
        tags: entry.tags || ["wall-texture"],
      };

      // upload texture map as 'texture'
      const textureAsset = await uploadAssetFile({
        productId: product.id,
        assetType: "texture",
        absolutePath: textureAbs,
        metadata: baseMetadata,
      });
      await createAssetRecord({ productId: product.id, ...textureAsset });
      summary.uploaded += 1;

      // upload thumbnail as 'thumbnail'
      const thumbAsset = await uploadAssetFile({
        productId: product.id,
        assetType: "thumbnail",
        absolutePath: thumbnailAbs,
        metadata: {
          ...baseMetadata,
          role: "primary",
          datasetPath: entry.thumbnailPath,
        },
      });
      await createAssetRecord({ productId: product.id, ...thumbAsset });
      summary.uploaded += 1;

      console.log(`[IMPORTED] ${name} -> product #${product.id}`);
    } catch (err) {
      console.error(
        `[FAILED] ${entry.name || entry.textureMapPath}: ${err.message}`,
      );
      summary.failed += 1;
    }
  }

  console.log("\nWall textures import summary:");
  console.log(JSON.stringify(summary, null, 2));
};

main()
  .catch((err) => {
    console.error(err.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
