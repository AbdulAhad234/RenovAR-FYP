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
const { DEFAULT_CURRENCY } = require("../src/constants/currency");
const { getProductDescription } = require("./product-copy");

const DEFAULT_MANIFEST_PATH = path.resolve(
  __dirname,
  "../../3D_Models_Dataset/curated-manifest.json",
);
const DEFAULT_CATEGORY_MAPPING = {
  accessories: "decor",
  beds: "bedroom",
  chairs: "living-room",
  sofas: "living-room",
  tables: "dining-room",
  "coffee tables": "living-room",
};

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

const parseArgs = (argv) => {
  const args = {
    manifest: DEFAULT_MANIFEST_PATH,
    dryRun: false,
    skipDuplicates: false,
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];

    if (arg === "--manifest") {
      args.manifest = path.resolve(process.cwd(), argv[index + 1]);
      index += 1;
    } else if (arg === "--dry-run") {
      args.dryRun = true;
    } else if (arg === "--skip-duplicates") {
      args.skipDuplicates = true;
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

const getMimeType = (filePath, assetType) => {
  const extension = path.extname(filePath).toLowerCase();

  if (assetType === "model") {
    return MODEL_MIME_TYPES[extension] || null;
  }

  return IMAGE_MIME_TYPES[extension] || null;
};

const normalizeCategorySlug = (entry, manifest) => {
  if (entry.normalizedCategorySlug) {
    return entry.normalizedCategorySlug;
  }

  const manifestMapping = Object.entries(manifest.categoryMapping || {}).reduce(
    (accumulator, [key, value]) => {
      accumulator[String(key).trim().toLowerCase()] = value;
      return accumulator;
    },
    {},
  );

  const categoryMapping = {
    ...DEFAULT_CATEGORY_MAPPING,
    ...manifestMapping,
  };

  const rawCategory = (entry.sourceCategory || entry.category || "")
    .trim()
    .toLowerCase();
  return categoryMapping[rawCategory] || null;
};

const ensureFileExists = (absolutePath) => {
  if (!fs.existsSync(absolutePath)) {
    throw new Error(`File not found: ${absolutePath}`);
  }
};

const loadManifest = (manifestPath) => {
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Manifest file not found: ${manifestPath}`);
  }

  const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  if (!Array.isArray(manifest.products)) {
    throw new Error('Manifest must contain a "products" array');
  }

  return manifest;
};

const resolveDatasetPath = (datasetRoot, relativePath) => {
  return path.resolve(datasetRoot, relativePath);
};

const findVendorByEmail = async (email) => {
  const result = await query(
    `
      SELECT
        u.id,
        u.email,
        u.role,
        u.display_name AS "displayName",
        vp.company_name AS "companyName"
      FROM users u
      LEFT JOIN vendor_profiles vp ON vp.user_id = u.id
      WHERE LOWER(u.email) = LOWER($1)
      LIMIT 1
    `,
    [email],
  );

  return result.rows[0] || null;
};

const findCategoryBySlug = async (slug) => {
  const result = await query(
    `
      SELECT id, name, slug
      FROM categories
      WHERE slug = $1
      LIMIT 1
    `,
    [slug],
  );

  return result.rows[0] || null;
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

const findExistingProduct = async (vendorId, productName, categoryId) => {
  const result = await query(
    `
      SELECT id, name
      FROM products
      WHERE vendor_id = $1
        AND LOWER(name) = LOWER($2)
        AND category_id = $3
      LIMIT 1
    `,
    [vendorId, productName, categoryId],
  );

  return result.rows[0] || null;
};

const createProductRecord = async (payload) => {
  const result = await query(
    `
      INSERT INTO products (vendor_id, category_id, name, brand_name, description, texture_target, price, dimensions_json, status)
      VALUES ($1, $2, $3, $4, $5, $6, $7, $8, 'draft')
      RETURNING id, name
    `,
    [
      payload.vendorId,
      payload.categoryId,
      payload.name,
      payload.brandName || null,
      payload.description || null,
      payload.textureTarget || null,
      payload.price,
      payload.dimensions || null,
    ],
  );

  return result.rows[0];
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

const uploadAssetFile = async ({
  productId,
  assetType,
  absolutePath,
  metadata,
}) => {
  const fileBuffer = fs.readFileSync(absolutePath);
  const fileName = path.basename(absolutePath);
  const mimeType = getMimeType(absolutePath, assetType);

  if (!mimeType) {
    throw new Error(`Unsupported ${assetType} file type: ${absolutePath}`);
  }

  const s3Key = generateS3Key(String(productId), assetType, fileName);
  await uploadToS3(fileBuffer, s3Key, mimeType);

  return {
    assetType,
    s3Key,
    s3Url: buildS3Url(s3Key),
    fileName,
    fileSize: fileBuffer.length,
    mimeType,
    metadata: {
      ...metadata,
      format: path.extname(fileName).replace(".", "").toLowerCase(),
      originalFileName: fileName,
    },
  };
};

const importEntry = async ({
  entry,
  index,
  manifest,
  datasetRoot,
  vendors,
  options,
  summary,
}) => {
  const vendor = pickVendorForEntry(entry, vendors, index);
  const brandName =
    entry.brandName ||
    vendor.brandName ||
    manifest.defaultBrandName ||
    vendor.companyName ||
    vendor.displayName ||
    null;
  const categorySlug = normalizeCategorySlug(entry, manifest);
  if (!categorySlug) {
    throw new Error(`Could not normalize category for product "${entry.name}"`);
  }

  const category = await findCategoryBySlug(categorySlug);
  if (!category) {
    throw new Error(`Category slug not found in database: ${categorySlug}`);
  }

  const modelAbsolutePath = resolveDatasetPath(datasetRoot, entry.modelPath);
  const primaryImageAbsolutePath = resolveDatasetPath(
    datasetRoot,
    entry.primaryImagePath,
  );
  const additionalImagePaths = Array.isArray(entry.additionalImagePaths)
    ? entry.additionalImagePaths
    : [];

  ensureFileExists(modelAbsolutePath);
  ensureFileExists(primaryImageAbsolutePath);
  additionalImagePaths.map((relativePath) =>
    ensureFileExists(resolveDatasetPath(datasetRoot, relativePath)),
  );

  const existingProduct = await findExistingProduct(
    vendor.id,
    entry.name,
    category.id,
  );
  if (existingProduct && options.skipDuplicates) {
    summary.skipped += 1;
    console.log(
      `[SKIP] ${entry.name} already exists as product #${existingProduct.id}`,
    );
    return;
  }

  const productPreview = {
    name: entry.name,
    brandName,
    vendorEmail: vendor.email,
    categorySlug,
    price: Number(entry.price),
    currency: entry.currency || manifest.currency || DEFAULT_CURRENCY,
    modelPath: entry.modelPath,
    primaryImagePath: entry.primaryImagePath,
  };
  summary.byVendor[vendor.email] = (summary.byVendor[vendor.email] || 0) + 1;

  if (options.dryRun) {
    summary.previewed += 1;
    console.log(
      `[DRY-RUN] Would import product: ${JSON.stringify(productPreview)}`,
    );
    return;
  }

  const product =
    existingProduct ||
    (await createProductRecord({
      vendorId: vendor.id,
      categoryId: category.id,
      name: entry.name,
      brandName,
      description: getProductDescription(entry),
      textureTarget: entry.textureTarget || null,
      price: Number(entry.price),
      dimensions: entry.dimensions || null,
    }));

  if (!existingProduct) {
    summary.productsCreated += 1;
  }

  const baseMetadata = {
    importSource: "curated-dataset-manifest",
    manifestKey: entry.importKey || slugify(entry.name),
    sourceCategory: entry.sourceCategory || entry.category || null,
    brandName,
    tags: entry.tags || [],
    currency: entry.currency || manifest.currency || DEFAULT_CURRENCY,
    originalModelPath: entry.sourceModelPath || null,
    originalPrimaryImagePath: entry.sourcePrimaryImagePath || null,
    originalAdditionalImagePaths: entry.sourceAdditionalImagePaths || [],
    curationConfidence: entry.confidence || null,
  };

  const modelAsset = await uploadAssetFile({
    productId: product.id,
    assetType: "model",
    absolutePath: modelAbsolutePath,
    metadata: {
      ...baseMetadata,
      datasetPath: entry.modelPath,
    },
  });

  await createAssetRecord({
    productId: product.id,
    ...modelAsset,
  });
  summary.assetsUploaded += 1;

  const thumbnailAsset = await uploadAssetFile({
    productId: product.id,
    assetType: "thumbnail",
    absolutePath: primaryImageAbsolutePath,
    metadata: {
      ...baseMetadata,
      datasetPath: entry.primaryImagePath,
      role: "primary",
    },
  });

  await createAssetRecord({
    productId: product.id,
    ...thumbnailAsset,
  });
  summary.assetsUploaded += 1;

  for (const relativePath of additionalImagePaths) {
    const imageAsset = await uploadAssetFile({
      productId: product.id,
      assetType: "image",
      absolutePath: resolveDatasetPath(datasetRoot, relativePath),
      metadata: {
        ...baseMetadata,
        datasetPath: relativePath,
        role: "gallery",
      },
    });

    await createAssetRecord({
      productId: product.id,
      ...imageAsset,
    });
    summary.assetsUploaded += 1;
  }

  console.log(`[IMPORTED] ${entry.name} -> product #${product.id}`);
};

const main = async () => {
  const options = parseArgs(process.argv.slice(2));
  const manifest = loadManifest(options.manifest);
  const datasetRoot = path.dirname(options.manifest);
  const vendors = await loadImportVendors(manifest);

  const summary = {
    previewed: 0,
    productsCreated: 0,
    assetsUploaded: 0,
    skipped: 0,
    failed: 0,
    byVendor: {},
  };

  for (const [index, entry] of manifest.products.entries()) {
    try {
      await importEntry({
        entry,
        index,
        manifest,
        datasetRoot,
        vendors,
        options,
        summary,
      });
    } catch (error) {
      summary.failed += 1;
      console.error(
        `[FAILED] ${entry.name || entry.modelPath || "unknown-entry"}: ${error.message}`,
      );
    }
  }

  console.log("\nImport summary");
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
