#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const {
  getProductDescription,
  getWallTextureDescription,
  getFloorTextureDescription,
} = require("./product-copy");
const { buildMaterialMaps } = require("./material-map-utils");

const DATASET_ROOT = path.resolve(__dirname, "../../3D_Models_Dataset");
const IMAGES_ROOT = path.join(DATASET_ROOT, "Images");
const MODELS_ROOT = path.join(DATASET_ROOT, "3D models");
const WALL_TEXTURE_MODELS_ROOT = path.join(MODELS_ROOT, "Wall Textures");
const WALL_TEXTURE_IMAGES_ROOT = path.join(IMAGES_ROOT, "Wall Textures");
const FLOOR_TEXTURE_IMAGES_ROOT = path.join(IMAGES_ROOT, "Floor Textures");
const RENAME_MAP_PATH = path.join(DATASET_ROOT, "rename-map.json");
const CURATION_REVIEW_PATH = path.join(DATASET_ROOT, "curation-review.json");
const MANIFEST_PATH = path.join(DATASET_ROOT, "curated-manifest.json");

const USD_TO_PKR_RATE = 279.8;

const CATEGORY_MAPPING = {
  Accessories: "decor",
  Beds: "bedroom",
  Chairs: "living-room",
  Sofas: "living-room",
  Tables: "dining-room",
  "Coffee tables": "living-room",
};

const CATEGORY_PRICE_PKR = {
  Accessories: 35000,
  Beds: 335000,
  Chairs: 70000,
  Sofas: 280000,
  Tables: 210000,
};

const KNOWN_USD_PRICES = {
  "ladder-bookshelf": 249.99,
  "wooden-china-cabinet": 699.99,
  "wooden-pendant-light": 129.99,
  "classic-wooden-bed": 1199.99,
  "storage-platform-bed": 1399.99,
  "leather-dining-chair": 249.99,
  "lounge-accent-chair": 329.99,
  "modern-accent-armchair": 449.99,
  "leather-chesterfield-sofa": 1299.99,
  "tufted-living-sofa": 999.99,
  "antique-wooden-table": 849.99,
  "wooden-dining-table": 749.99,
  "farmhouse-wooden-table": 699.99,
};

const DATASET_VENDORS = [
  {
    firebaseUid: "dataset-vendor-urban-living-studio",
    email: "urban.living@renovar.local",
    displayName: "Urban Living Studio",
    companyName: "Urban Living Studio",
    brandName: "Urban Living Studio",
  },
  {
    firebaseUid: "dataset-vendor-heritage-home-co",
    email: "heritage.home@renovar.local",
    displayName: "Heritage Home Co",
    companyName: "Heritage Home Co",
    brandName: "Heritage Home Co",
  },
  {
    firebaseUid: "dataset-vendor-minimal-nest",
    email: "minimal.nest@renovar.local",
    displayName: "Minimal Nest",
    companyName: "Minimal Nest",
    brandName: "Minimal Nest",
  },
  {
    firebaseUid: "dataset-vendor-wallcraft-surfaces",
    email: "wallcraft.surfaces@renovar.local",
    displayName: "Wallcraft Surfaces",
    companyName: "Wallcraft Surfaces",
    brandName: "Wallcraft Surfaces",
  },
];

const IMAGE_EXTENSIONS = new Set([".jpg", ".jpeg", ".png", ".webp"]);
const MODEL_EXTENSIONS = new Set([".glb", ".fbx"]);
const WALL_TEXTURE_EXTENSIONS = new Set([
  ".jpg",
  ".jpeg",
  ".png",
  ".webp",
  ".tif",
  ".tiff",
]);

const normalizePath = (value) => String(value).replace(/\\/g, "/");

const relativeDatasetPath = (absolutePath) => {
  return normalizePath(path.relative(DATASET_ROOT, absolutePath));
};

const slugToTitle = (slug) => {
  return slug
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
};

const normalizeSlug = (value) => {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/&/g, "and")
    .replace(/\+/g, "-")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/-primary$/i, "")
    .replace(/^-+|-+$/g, "");
};

const listFiles = (rootPath) => {
  const output = [];
  if (!fs.existsSync(rootPath)) {
    return output;
  }

  const visit = (currentPath) => {
    const entries = fs.readdirSync(currentPath, { withFileTypes: true });
    for (const entry of entries) {
      const absolutePath = path.join(currentPath, entry.name);
      if (entry.isDirectory()) {
        visit(absolutePath);
      } else if (entry.isFile()) {
        output.push(absolutePath);
      }
    }
  };

  visit(rootPath);
  return output;
};

const getCategory = (absolutePath) => {
  return path.basename(path.dirname(absolutePath));
};

const getBaseSlug = (absolutePath) => {
  return normalizeSlug(path.basename(absolutePath, path.extname(absolutePath)));
};

const buildImageIndex = () => {
  const imageFiles = listFiles(IMAGES_ROOT)
    .filter((filePath) =>
      IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
    )
    .filter(
      (filePath) =>
        !relativeDatasetPath(filePath).startsWith("Images/Wall Textures/"),
    );

  const index = new Map();
  for (const imagePath of imageFiles) {
    const key = `${getCategory(imagePath).toLowerCase()}::${getBaseSlug(imagePath)}`;
    if (!index.has(key)) {
      index.set(key, []);
    }
    index.get(key).push(imagePath);
  }

  return { imageFiles, index };
};

const getProductPrice = (slug, category) => {
  if (KNOWN_USD_PRICES[slug] !== undefined) {
    return Math.round(KNOWN_USD_PRICES[slug] * USD_TO_PKR_RATE);
  }

  return CATEGORY_PRICE_PKR[category] || 50000;
};

const buildProductPairs = () => {
  const { imageFiles, index } = buildImageIndex();
  const modelFiles = listFiles(MODELS_ROOT)
    .filter((filePath) =>
      MODEL_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
    )
    .filter(
      (filePath) =>
        !relativeDatasetPath(filePath).startsWith("3D models/Wall Textures/"),
    );

  const usedImages = new Set();
  const products = [];
  const unmatchedModels = [];

  for (const modelPath of modelFiles.sort((a, b) =>
    relativeDatasetPath(a).localeCompare(relativeDatasetPath(b)),
  )) {
    const category = getCategory(modelPath);
    const slug = getBaseSlug(modelPath);
    const key = `${category.toLowerCase()}::${slug}`;
    const candidates = index.get(key) || [];
    const chosenImage = candidates.find(
      (imagePath) => !usedImages.has(imagePath),
    );

    if (!chosenImage) {
      unmatchedModels.push(relativeDatasetPath(modelPath));
      continue;
    }

    usedImages.add(chosenImage);
    const vendor = DATASET_VENDORS[products.length % DATASET_VENDORS.length];
    const productEntry = {
      importKey: `dataset-${slug}-001`,
      name: slugToTitle(slug),
      vendorEmail: vendor.email,
      brandName: vendor.brandName,
      sourceCategory: category,
      normalizedCategorySlug:
        CATEGORY_MAPPING[category] || normalizeSlug(category),
      modelPath: relativeDatasetPath(modelPath),
      primaryImagePath: relativeDatasetPath(chosenImage),
      additionalImagePaths: [],
      sourceModelPath: relativeDatasetPath(modelPath),
      sourcePrimaryImagePath: relativeDatasetPath(chosenImage),
      sourceAdditionalImagePaths: [],
      confidence: "high",
      price: getProductPrice(slug, category),
      currency: "PKR",
      dimensions: { unit: "cm" },
      tags: ["dataset-import", normalizeSlug(category), slug],
    };

    productEntry.description = getProductDescription(productEntry);
    products.push(productEntry);
  }

  const unmatchedImages = imageFiles
    .filter((imagePath) => !usedImages.has(imagePath))
    .map(relativeDatasetPath)
    .sort();

  return {
    products,
    modelFiles,
    imageFiles,
    unmatchedModels,
    unmatchedImages,
  };
};

const pickWallTextureMap = (textureFiles) => {
  const materialMaps = buildMaterialMaps(textureFiles);
  return materialMaps.baseColor || null;
};

const buildWallTexturePairs = () => {
  // Prefer model-folder-based wall textures when available
  if (fs.existsSync(WALL_TEXTURE_MODELS_ROOT)) {
    const entries = fs
      .readdirSync(WALL_TEXTURE_MODELS_ROOT, { withFileTypes: true })
      .filter((entry) => entry.isDirectory())
      .map((entry, index) => {
        const slug = normalizeSlug(entry.name);
        const vendor = DATASET_VENDORS[index % DATASET_VENDORS.length];
        const textureFolder = path.join(WALL_TEXTURE_MODELS_ROOT, entry.name);
        const textureFiles = listFiles(textureFolder).filter((filePath) =>
          WALL_TEXTURE_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
        );
        const thumbnailCandidates = listFiles(WALL_TEXTURE_IMAGES_ROOT)
          .filter((filePath) =>
            IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
          )
          .filter((filePath) => getBaseSlug(filePath) === slug);
        const materialMaps = buildMaterialMaps(
          textureFiles,
          relativeDatasetPath,
        );
        const textureMap = materialMaps.baseColor;

        if (!textureMap || thumbnailCandidates.length === 0) {
          return null;
        }

        const wallTextureEntry = {
          importKey: `wall-texture-${slug}-001`,
          name: slugToTitle(slug),
          vendorEmail: vendor.email,
          brandName: vendor.brandName,
          textureFolderPath: relativeDatasetPath(textureFolder),
          textureMapPath: textureMap,
          materialMaps,
          thumbnailPath: relativeDatasetPath(thumbnailCandidates[0]),
          mapRole: "baseColor",
          fileUse:
            "Use textureMapPath as the AR material base color map. Use thumbnailPath only for product browsing and selection UI.",
          textureTarget: "wall",
          currency: "PKR",
          tags: ["wall-texture", slug],
        };

        wallTextureEntry.description =
          getWallTextureDescription(wallTextureEntry);
        return wallTextureEntry;
      })
      .filter(Boolean)
      .sort((a, b) => a.name.localeCompare(b.name));

    if (entries.length > 0) return entries;
  }

  // Fallback: build wall texture entries directly from Images/Wall Textures
  if (!fs.existsSync(WALL_TEXTURE_IMAGES_ROOT)) return [];

  const imageFiles = listFiles(WALL_TEXTURE_IMAGES_ROOT).filter((filePath) =>
    IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
  );

  const entries = imageFiles.map((absPath, index) => {
    const slug = normalizeSlug(getBaseSlug(absPath));
    const vendor = DATASET_VENDORS[index % DATASET_VENDORS.length];
    const relPath = relativeDatasetPath(absPath);

    const entry = {
      importKey: `wall-texture-${slug}-001`,
      name: slugToTitle(slug),
      vendorEmail: vendor.email,
      brandName: vendor.brandName,
      textureMapPath: relPath,
      materialMaps: { baseColor: relPath },
      thumbnailPath: relPath,
      mapRole: "baseColor",
      fileUse:
        "Use textureMapPath as the AR material base color map. Use thumbnailPath only for product browsing and selection UI.",
      textureTarget: "wall",
      currency: "PKR",
      tags: ["wall-texture", slug],
    };

    entry.description = getWallTextureDescription(entry);
    return entry;
  });

  return entries.sort((a, b) => a.name.localeCompare(b.name));
};

const buildFloorTexturePairs = () => {
  if (!fs.existsSync(FLOOR_TEXTURE_IMAGES_ROOT)) return [];

  const imageFiles = listFiles(FLOOR_TEXTURE_IMAGES_ROOT).filter((filePath) =>
    IMAGE_EXTENSIONS.has(path.extname(filePath).toLowerCase()),
  );

  const entries = imageFiles.map((absPath, index) => {
    const slug = normalizeSlug(getBaseSlug(absPath));
    const vendor = DATASET_VENDORS[index % DATASET_VENDORS.length];
    const relPath = relativeDatasetPath(absPath);

    const entry = {
      importKey: `floor-texture-${slug}-001`,
      name: slugToTitle(slug),
      vendorEmail: vendor.email,
      brandName: vendor.brandName,
      textureMapPath: relPath,
      materialMaps: { baseColor: relPath },
      thumbnailPath: relPath,
      mapRole: "baseColor",
      fileUse:
        "Use textureMapPath as the AR material base color map. Use thumbnailPath only for product browsing and selection UI.",
      textureTarget: "floor",
      currency: "PKR",
      tags: ["floor-texture", slug],
    };

    entry.description = getFloorTextureDescription
      ? getFloorTextureDescription(entry)
      : getWallTextureDescription(entry);
    return entry;
  });

  return entries.sort((a, b) => a.name.localeCompare(b.name));
};

const buildManifest = () => {
  const paired = buildProductPairs();
  const wallTextures = buildWallTexturePairs();
  const floorTextures = buildFloorTexturePairs();

  return {
    defaultVendorEmail: "renovar.test1@gmail.com",
    defaultBrandName: "RenovAR Curated",
    datasetVendors: DATASET_VENDORS,
    currency: "PKR",
    currencySymbol: "Rs",
    priceMetadata: {
      sourceCurrency: "USD",
      targetCurrency: "PKR",
      usdToPkrRate: USD_TO_PKR_RATE,
      rateDate: "2026-05-02",
      note: "Known curated USD prices were converted to PKR; newly paired products use category-level PKR defaults.",
    },
    categoryMapping: CATEGORY_MAPPING,
    metadata: {
      generatedAt: new Date().toISOString(),
      appliedMethod: "same-basename-category-pairing",
      productsPaired: paired.products.length,
      wallTexturesPaired: wallTextures.length,
      floorTexturesPaired: floorTextures.length,
    },
    products: paired.products,
    wallTextures,
    floorTextures,
  };
};

const buildRenameMap = (manifest) => {
  const mappedProductPaths = new Set();
  const items = [];

  for (const product of manifest.products) {
    mappedProductPaths.add(product.modelPath);
    mappedProductPaths.add(product.primaryImagePath);
    items.push({
      originalPath: product.modelPath,
      newPath: product.modelPath,
      category: product.sourceCategory,
      productSlug: normalizeSlug(product.name),
      assetKind: "model",
      status: "paired",
      confidence: product.confidence,
    });
    items.push({
      originalPath: product.primaryImagePath,
      newPath: product.primaryImagePath,
      category: product.sourceCategory,
      productSlug: normalizeSlug(product.name),
      sequence: "primary",
      assetKind: "image",
      status: "paired",
      confidence: product.confidence,
    });
  }

  for (const wallTexture of manifest.wallTextures) {
    mappedProductPaths.add(wallTexture.textureMapPath);
    mappedProductPaths.add(wallTexture.thumbnailPath);
    items.push({
      originalPath: wallTexture.textureMapPath,
      newPath: wallTexture.textureMapPath,
      category: "Wall Textures",
      productSlug: normalizeSlug(wallTexture.name),
      assetKind: "wall-texture-map",
      status: "paired",
      confidence: "high",
    });
    items.push({
      originalPath: wallTexture.thumbnailPath,
      newPath: wallTexture.thumbnailPath,
      category: "Wall Textures",
      productSlug: normalizeSlug(wallTexture.name),
      assetKind: "wall-texture-thumbnail",
      status: "paired",
      confidence: "high",
    });
  }

  const allDatasetAssets = [
    ...listFiles(IMAGES_ROOT).map(relativeDatasetPath),
    ...listFiles(MODELS_ROOT).map(relativeDatasetPath),
  ];

  for (const datasetPath of allDatasetAssets.sort()) {
    if (mappedProductPaths.has(datasetPath)) {
      continue;
    }

    items.push({
      originalPath: datasetPath,
      newPath: null,
      category: normalizePath(path.dirname(datasetPath)).split("/")[1] || null,
      productSlug: null,
      assetKind: datasetPath.startsWith("Images/") ? "image" : "model",
      status: "unmatched",
      confidence: null,
    });
  }

  return {
    generatedAt: new Date().toISOString(),
    datasetRoot: DATASET_ROOT,
    summary: {
      pairedProducts: manifest.products.length,
      pairedWallTextures: manifest.wallTextures.length,
      pairedFloorTextures: (manifest.floorTextures || []).length,
      totalItemsInventoried: items.length,
    },
    items,
  };
};

const buildReviewArtifact = (manifest) => {
  return {
    generatedAt: new Date().toISOString(),
    productPairs: manifest.products.map((product) => ({
      importKey: product.importKey,
      name: product.name,
      brandName: product.brandName || manifest.defaultBrandName || null,
      sourceCategory: product.sourceCategory,
      modelPath: product.modelPath,
      primaryImagePath: product.primaryImagePath,
      price: product.price,
      currency: product.currency,
    })),
    wallTexturePairs: manifest.wallTextures.map((texture) => ({
      importKey: texture.importKey,
      name: texture.name,
      textureFolderPath: texture.textureFolderPath,
      textureMapPath: texture.textureMapPath,
      thumbnailPath: texture.thumbnailPath,
      mapRole: texture.mapRole,
    })),
    floorTexturePairs: (manifest.floorTextures || []).map((texture) => ({
      importKey: texture.importKey,
      name: texture.name,
      textureMapPath: texture.textureMapPath,
      thumbnailPath: texture.thumbnailPath,
      mapRole: texture.mapRole || "baseColor",
    })),
  };
};

const writeJson = (targetPath, payload) => {
  fs.writeFileSync(targetPath, `${JSON.stringify(payload, null, 2)}\n`, "utf8");
};

const main = () => {
  const manifest = buildManifest();
  const renameMap = buildRenameMap(manifest);
  const reviewArtifact = buildReviewArtifact(manifest);

  writeJson(RENAME_MAP_PATH, renameMap);
  writeJson(CURATION_REVIEW_PATH, reviewArtifact);
  writeJson(MANIFEST_PATH, manifest);

  console.log(
    `Prepared ${manifest.products.length} product pairs and ${manifest.wallTextures.length} wall texture pairs.`,
  );
  console.log(
    "No files were renamed; pairs are derived from same-name model/image assets.",
  );
};

main();
