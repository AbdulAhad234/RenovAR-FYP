#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

const DATASET_ROOT = path.resolve(__dirname, '../../3D_Models_Dataset');
const IMAGES_ROOT = path.join(DATASET_ROOT, 'Images');
const MODELS_ROOT = path.join(DATASET_ROOT, '3D models');
const RENAME_MAP_PATH = path.join(DATASET_ROOT, 'rename-map.json');
const CURATION_REVIEW_PATH = path.join(DATASET_ROOT, 'curation-review.json');
const MANIFEST_PATH = path.join(DATASET_ROOT, 'curated-manifest.json');

const ML_RECOMMENDATION = {
  preferred: 'SigLIP 2',
  fallback: ['BLIP-2', 'CLIP'],
  appliedMethod: 'manual-reviewed-visual-curation',
  note: 'The local workspace does not include an installed vision-model runtime, so this curated set was reviewed visually and remains traceable through the generated artifacts.'
};

const CURATED_PRODUCTS = [
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-ladder-bookshelf-001',
    productSlug: 'ladder-bookshelf',
    name: 'Ladder Bookshelf',
    sourceCategory: 'Accessories',
    normalizedCategorySlug: 'decor',
    modelOriginalPath: '3D models/Accessories/ladder+bookshelf+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Accessories/Engineered_Wood_21.jpg',
    additionalImageOriginalPaths: [],
    price: 249.99,
    description: 'Curated ladder-style bookshelf selected for the decor launch set.',
    dimensions: { width: 52, depth: 36, height: 170, unit: 'cm' },
    tags: ['curated-launch', 'decor', 'bookshelf', 'wood']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-wooden-china-cabinet-001',
    productSlug: 'wooden-china-cabinet',
    name: 'Wooden China Cabinet',
    sourceCategory: 'Accessories',
    normalizedCategorySlug: 'decor',
    modelOriginalPath: '3D models/Accessories/wooden+china+cabinet+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Accessories/Engineered_Wood_2.jpg',
    additionalImageOriginalPaths: [],
    price: 699.99,
    description: 'Curated wooden china cabinet chosen for the decor and storage catalog.',
    dimensions: { width: 122, depth: 42, height: 188, unit: 'cm' },
    tags: ['curated-launch', 'decor', 'cabinet', 'storage']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-wooden-pendant-light-001',
    productSlug: 'wooden-pendant-light',
    name: 'Wooden Pendant Light',
    sourceCategory: 'Accessories',
    normalizedCategorySlug: 'decor',
    modelOriginalPath: '3D models/Accessories/wooden+pendant+light+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Accessories/Engineered_Wood_20.jpg',
    additionalImageOriginalPaths: [],
    price: 129.99,
    description: 'Curated pendant light fixture selected for the decor launch set.',
    dimensions: { width: 28, depth: 28, height: 70, unit: 'cm' },
    tags: ['curated-launch', 'decor', 'lighting', 'wood']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-classic-wooden-bed-001',
    productSlug: 'classic-wooden-bed',
    name: 'Classic Wooden Bed',
    sourceCategory: 'Beds',
    normalizedCategorySlug: 'bedroom',
    modelOriginalPath: '3D models/Beds/bed+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Beds/Engineered_Wood_Bed_1.jpg',
    additionalImageOriginalPaths: [],
    price: 1199.99,
    description: 'Curated wooden platform bed for the bedroom launch set.',
    dimensions: { width: 180, depth: 210, height: 100, unit: 'cm' },
    tags: ['curated-launch', 'bed', 'bedroom', 'wood']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-storage-platform-bed-001',
    productSlug: 'storage-platform-bed',
    name: 'Storage Platform Bed',
    sourceCategory: 'Beds',
    normalizedCategorySlug: 'bedroom',
    modelOriginalPath: '3D models/Beds/bed+3d+model (1).fbx',
    primaryImageOriginalPath: 'Images/Beds/King_With_Storage_11.jpg',
    additionalImageOriginalPaths: [],
    price: 1399.99,
    description: 'Curated platform bed with storage styling for the bedroom catalog.',
    dimensions: { width: 190, depth: 220, height: 118, unit: 'cm' },
    tags: ['curated-launch', 'bed', 'bedroom', 'storage']
  },
  {
    status: 'skipped',
    confidence: 'low',
    productSlug: 'tree-canopy-bed',
    name: 'Tree Canopy Bed',
    sourceCategory: 'Beds',
    normalizedCategorySlug: 'bedroom',
    modelOriginalPath: '3D models/Beds/tree+canopy+bed+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Beds/Kids/Kids_Bed_3.jpg',
    additionalImageOriginalPaths: [],
    skipReason: 'No high-confidence canopy-style image was present in the local image set.'
  },
  {
    status: 'approved',
    confidence: 'medium',
    importKey: 'curated-leather-dining-chair-001',
    productSlug: 'leather-dining-chair',
    name: 'Leather Dining Chair',
    sourceCategory: 'Chairs',
    normalizedCategorySlug: 'living-room',
    modelOriginalPath: '3D models/chairs/leather+chair+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Chairs/00000200.jpg',
    additionalImageOriginalPaths: [],
    price: 249.99,
    description: 'Curated dining-style leather chair for the seating launch set.',
    dimensions: { width: 48, depth: 56, height: 96, unit: 'cm' },
    tags: ['curated-launch', 'chair', 'leather', 'dining']
  },
  {
    status: 'approved',
    confidence: 'medium',
    importKey: 'curated-lounge-accent-chair-001',
    productSlug: 'lounge-accent-chair',
    name: 'Lounge Accent Chair',
    sourceCategory: 'Chairs',
    normalizedCategorySlug: 'living-room',
    modelOriginalPath: '3D models/chairs/outdoor+lounge+chair+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Chairs/00000173.jpg',
    additionalImageOriginalPaths: [],
    price: 329.99,
    description: 'Curated lounge chair used as the closest reviewed match for the outdoor lounge model.',
    dimensions: { width: 70, depth: 78, height: 98, unit: 'cm' },
    tags: ['curated-launch', 'chair', 'lounge', 'accent']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-modern-accent-armchair-001',
    productSlug: 'modern-accent-armchair',
    name: 'Modern Accent Armchair',
    sourceCategory: 'Sofas',
    normalizedCategorySlug: 'living-room',
    modelOriginalPath: '3D models/Sofas/armchair+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Sofas/00000020.jpg',
    additionalImageOriginalPaths: [],
    price: 449.99,
    description: 'Curated armchair selected as the living room accent seating model.',
    dimensions: { width: 84, depth: 86, height: 92, unit: 'cm' },
    tags: ['curated-launch', 'armchair', 'accent', 'living-room']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-leather-chesterfield-sofa-001',
    productSlug: 'leather-chesterfield-sofa',
    name: 'Leather Chesterfield Sofa',
    sourceCategory: 'Sofas',
    normalizedCategorySlug: 'living-room',
    modelOriginalPath: '3D models/Sofas/leather+sofa+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Sofas/00000014.jpg',
    additionalImageOriginalPaths: [],
    price: 1299.99,
    description: 'Curated leather sofa selected for the living room launch set.',
    dimensions: { width: 220, depth: 92, height: 78, unit: 'cm' },
    tags: ['curated-launch', 'sofa', 'leather', 'living-room']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-tufted-living-sofa-001',
    productSlug: 'tufted-living-sofa',
    name: 'Tufted Living Sofa',
    sourceCategory: 'Sofas',
    normalizedCategorySlug: 'living-room',
    modelOriginalPath: '3D models/Sofas/sofa+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Sofas/00000069.jpg',
    additionalImageOriginalPaths: [],
    price: 999.99,
    description: 'Curated tufted sofa for the core living room catalog.',
    dimensions: { width: 210, depth: 90, height: 82, unit: 'cm' },
    tags: ['curated-launch', 'sofa', 'tufted', 'living-room']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-antique-wooden-table-001',
    productSlug: 'antique-wooden-table',
    name: 'Antique Wooden Table',
    sourceCategory: 'Tables',
    normalizedCategorySlug: 'dining-room',
    modelOriginalPath: '3D models/tables/antique+wooden+table+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Tables/00000382.jpg',
    additionalImageOriginalPaths: [],
    price: 849.99,
    description: 'Curated antique wooden table selected for the dining catalog.',
    dimensions: { width: 190, depth: 92, height: 76, unit: 'cm' },
    tags: ['curated-launch', 'table', 'antique', 'wood']
  },
  {
    status: 'approved',
    confidence: 'high',
    importKey: 'curated-wooden-dining-table-001',
    productSlug: 'wooden-dining-table',
    name: 'Wooden Dining Table',
    sourceCategory: 'Tables',
    normalizedCategorySlug: 'dining-room',
    modelOriginalPath: '3D models/tables/wooden+dining+table+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Tables/00000111.jpg',
    additionalImageOriginalPaths: [],
    price: 749.99,
    description: 'Curated wooden dining table for the dining room launch set.',
    dimensions: { width: 180, depth: 90, height: 76, unit: 'cm' },
    tags: ['curated-launch', 'table', 'dining-room', 'wood']
  },
  {
    status: 'approved',
    confidence: 'medium',
    importKey: 'curated-farmhouse-wooden-table-001',
    productSlug: 'farmhouse-wooden-table',
    name: 'Farmhouse Wooden Table',
    sourceCategory: 'Tables',
    normalizedCategorySlug: 'dining-room',
    modelOriginalPath: '3D models/tables/wooden+table+3d+model.fbx',
    primaryImageOriginalPath: 'Images/Tables/00000069.jpg',
    additionalImageOriginalPaths: [],
    price: 699.99,
    description: 'Curated wooden table selected as the closest reviewed farmhouse-style match.',
    dimensions: { width: 175, depth: 88, height: 76, unit: 'cm' },
    tags: ['curated-launch', 'table', 'farmhouse', 'wood']
  }
];

const parseArgs = (argv) => {
  return {
    apply: argv.includes('--apply')
  };
};

const listFiles = (rootPath) => {
  const output = [];
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

const relativeDatasetPath = (absolutePath) => {
  return path.relative(DATASET_ROOT, absolutePath).split(path.sep).join('/');
};

const normalizePath = (value) => String(value).replace(/\\/g, '/');

const ensureExists = (relativePath) => {
  const absolutePath = path.join(DATASET_ROOT, relativePath);
  if (!fs.existsSync(absolutePath)) {
    throw new Error(`Expected dataset file was not found: ${relativePath}`);
  }
};

const targetAwareSourceExists = (sourcePath, targetPath) => {
  const sourceAbsolutePath = path.join(DATASET_ROOT, sourcePath);
  const targetAbsolutePath = targetPath ? path.join(DATASET_ROOT, targetPath) : null;
  return fs.existsSync(sourceAbsolutePath) || (targetAbsolutePath && fs.existsSync(targetAbsolutePath));
};

const buildApprovedRenameTargets = (entry) => {
  const modelCategoryFolder = path.posix.basename(path.posix.dirname(normalizePath(entry.modelOriginalPath)));
  const imageCategoryFolder = path.posix.basename(path.posix.dirname(normalizePath(entry.primaryImageOriginalPath)));
  const modelExtension = path.extname(entry.modelOriginalPath).toLowerCase() || '.fbx';
  const primaryExtension = path.extname(entry.primaryImageOriginalPath).toLowerCase() || '.jpg';
  const modelTargetPath = normalizePath(`3D models/${modelCategoryFolder}/${entry.productSlug}${modelExtension}`);
  const primaryImageTargetPath = normalizePath(`Images/${imageCategoryFolder}/${entry.productSlug}-primary${primaryExtension}`);
  const additionalImageTargetPaths = (entry.additionalImageOriginalPaths || []).map((imagePath, index) => {
    const extension = path.extname(imagePath).toLowerCase() || '.jpg';
    const extraImageFolder = path.posix.basename(path.posix.dirname(normalizePath(imagePath)));
    return normalizePath(`Images/${extraImageFolder}/${entry.productSlug}-${index + 1}${extension}`);
  });

  return {
    modelTargetPath,
    primaryImageTargetPath,
    additionalImageTargetPaths
  };
};

const validateReviewedPairs = () => {
  const seenModelPaths = new Set();
  const seenImagePaths = new Set();
  const seenTargetPaths = new Set();

  for (const entry of CURATED_PRODUCTS) {
    const targets = entry.status === 'approved' ? buildApprovedRenameTargets(entry) : null;

    if (entry.status === 'approved') {
      if (!targetAwareSourceExists(entry.modelOriginalPath, targets.modelTargetPath)) {
        throw new Error(`Expected dataset file was not found: ${entry.modelOriginalPath}`);
      }

      if (!targetAwareSourceExists(entry.primaryImageOriginalPath, targets.primaryImageTargetPath)) {
        throw new Error(`Expected dataset file was not found: ${entry.primaryImageOriginalPath}`);
      }

      (entry.additionalImageOriginalPaths || []).forEach((extraImage, index) => {
        if (!targetAwareSourceExists(extraImage, targets.additionalImageTargetPaths[index])) {
          throw new Error(`Expected dataset file was not found: ${extraImage}`);
        }
      });
    } else {
      ensureExists(entry.modelOriginalPath);
      ensureExists(entry.primaryImageOriginalPath);
      for (const extraImage of entry.additionalImageOriginalPaths || []) {
        ensureExists(extraImage);
      }
    }

    if (seenModelPaths.has(entry.modelOriginalPath)) {
      throw new Error(`Model path assigned twice: ${entry.modelOriginalPath}`);
    }
    seenModelPaths.add(entry.modelOriginalPath);

    if (entry.status === 'approved') {
      if (seenImagePaths.has(entry.primaryImageOriginalPath)) {
        throw new Error(`Primary image assigned twice: ${entry.primaryImageOriginalPath}`);
      }
      seenImagePaths.add(entry.primaryImageOriginalPath);

      for (const extraImage of entry.additionalImageOriginalPaths || []) {
        if (seenImagePaths.has(extraImage)) {
          throw new Error(`Additional image assigned twice: ${extraImage}`);
        }
        seenImagePaths.add(extraImage);
      }

      for (const targetPath of [targets.modelTargetPath, targets.primaryImageTargetPath, ...targets.additionalImageTargetPaths]) {
        if (seenTargetPaths.has(targetPath)) {
          throw new Error(`Generated target path is not unique: ${targetPath}`);
        }
        seenTargetPaths.add(targetPath);
      }
    }
  }
};

const buildRenameMap = () => {
  const approvedBySourcePath = new Map();

  for (const entry of CURATED_PRODUCTS) {
    if (entry.status !== 'approved') {
      continue;
    }

    const targets = buildApprovedRenameTargets(entry);
    approvedBySourcePath.set(normalizePath(entry.modelOriginalPath), {
      originalPath: normalizePath(entry.modelOriginalPath),
      newPath: targets.modelTargetPath,
      category: entry.sourceCategory,
      productSlug: entry.productSlug,
      sequence: null,
      assetKind: 'model',
      status: entry.status,
      confidence: entry.confidence
    });

    approvedBySourcePath.set(normalizePath(entry.primaryImageOriginalPath), {
      originalPath: normalizePath(entry.primaryImageOriginalPath),
      newPath: targets.primaryImageTargetPath,
      category: entry.sourceCategory,
      productSlug: entry.productSlug,
      sequence: 'primary',
      assetKind: 'image',
      status: entry.status,
      confidence: entry.confidence
    });

    (entry.additionalImageOriginalPaths || []).forEach((imagePath, index) => {
      approvedBySourcePath.set(normalizePath(imagePath), {
        originalPath: normalizePath(imagePath),
        newPath: targets.additionalImageTargetPaths[index],
        category: entry.sourceCategory,
        productSlug: entry.productSlug,
        sequence: index + 1,
        assetKind: 'image',
        status: entry.status,
        confidence: entry.confidence
      });
    });
  }

  const imageEntries = listFiles(IMAGES_ROOT)
    .map((absolutePath) => {
      const relativePath = relativeDatasetPath(absolutePath);
      const matched = approvedBySourcePath.get(relativePath);
      if (matched) {
        return matched;
      }

      const directoryParts = normalizePath(path.dirname(relativePath)).split('/');
      return {
        originalPath: relativePath,
        newPath: null,
        category: directoryParts[1] || null,
        productSlug: null,
        sequence: null,
        assetKind: 'image',
        status: 'unmatched',
        confidence: null
      };
    });

  const modelEntries = listFiles(MODELS_ROOT)
    .map((absolutePath) => {
      const relativePath = relativeDatasetPath(absolutePath);
      const matched = approvedBySourcePath.get(relativePath);
      if (matched) {
        return matched;
      }

      const directoryParts = normalizePath(path.dirname(relativePath)).split('/');
      return {
        originalPath: relativePath,
        newPath: null,
        category: directoryParts[1] || null,
        productSlug: null,
        sequence: null,
        assetKind: 'model',
        status: 'unmatched',
        confidence: null
      };
    });

  return {
    generatedAt: new Date().toISOString(),
    datasetRoot: DATASET_ROOT,
    recommendation: ML_RECOMMENDATION,
    summary: {
      imagesInventoried: imageEntries.length,
      modelsInventoried: modelEntries.length,
      approvedProducts: CURATED_PRODUCTS.filter((entry) => entry.status === 'approved').length,
      skippedProducts: CURATED_PRODUCTS.filter((entry) => entry.status === 'skipped').length
    },
    items: [...imageEntries, ...modelEntries]
  };
};

const buildManifest = () => {
  const approvedProducts = CURATED_PRODUCTS.filter((entry) => entry.status === 'approved');

  return {
    defaultVendorEmail: 'renovar.test1@gmail.com',
    categoryMapping: {
      Accessories: 'decor',
      Beds: 'bedroom',
      Chairs: 'living-room',
      Sofas: 'living-room',
      Tables: 'dining-room',
      'Coffee tables': 'living-room'
    },
    metadata: {
      generatedAt: new Date().toISOString(),
      matchingRecommendation: ML_RECOMMENDATION,
      reviewedSubset: approvedProducts.length
    },
    products: approvedProducts.map((entry) => {
      const targets = buildApprovedRenameTargets(entry);
      return {
        importKey: entry.importKey,
        name: entry.name,
        sourceCategory: entry.sourceCategory,
        normalizedCategorySlug: entry.normalizedCategorySlug,
        modelPath: targets.modelTargetPath,
        primaryImagePath: targets.primaryImageTargetPath,
        additionalImagePaths: targets.additionalImageTargetPaths,
        sourceModelPath: normalizePath(entry.modelOriginalPath),
        sourcePrimaryImagePath: normalizePath(entry.primaryImageOriginalPath),
        sourceAdditionalImagePaths: (entry.additionalImageOriginalPaths || []).map(normalizePath),
        confidence: entry.confidence,
        price: entry.price,
        description: entry.description,
        dimensions: entry.dimensions,
        tags: entry.tags
      };
    })
  };
};

const buildReviewArtifact = () => {
  const approved = CURATED_PRODUCTS.filter((entry) => entry.status === 'approved');
  const skipped = CURATED_PRODUCTS.filter((entry) => entry.status !== 'approved');

  return {
    generatedAt: new Date().toISOString(),
    recommendation: ML_RECOMMENDATION,
    approvedProducts: approved.map((entry) => ({
      productSlug: entry.productSlug,
      name: entry.name,
      sourceCategory: entry.sourceCategory,
      confidence: entry.confidence,
      modelOriginalPath: normalizePath(entry.modelOriginalPath),
      primaryImageOriginalPath: normalizePath(entry.primaryImageOriginalPath),
      additionalImageOriginalPaths: (entry.additionalImageOriginalPaths || []).map(normalizePath)
    })),
    skippedProducts: skipped.map((entry) => ({
      productSlug: entry.productSlug,
      name: entry.name,
      sourceCategory: entry.sourceCategory,
      confidence: entry.confidence,
      modelOriginalPath: normalizePath(entry.modelOriginalPath),
      candidateImagePath: normalizePath(entry.primaryImageOriginalPath),
      reason: entry.skipReason || 'Not approved for the current curated launch set.'
    }))
  };
};

const writeJson = (targetPath, payload) => {
  fs.writeFileSync(targetPath, `${JSON.stringify(payload, null, 2)}\n`, 'utf8');
};

const ensureParentDirectory = (absolutePath) => {
  fs.mkdirSync(path.dirname(absolutePath), { recursive: true });
};

const applyApprovedRenames = () => {
  const manifest = buildManifest();
  const renameSteps = [];

  for (const product of manifest.products) {
    renameSteps.push(
      { from: product.modelPath, source: product.sourceModelPath },
      { from: product.primaryImagePath, source: product.sourcePrimaryImagePath }
    );

    product.additionalImagePaths.forEach((targetPath, index) => {
      renameSteps.push({
        from: targetPath,
        source: product.sourceAdditionalImagePaths[index]
      });
    });
  }

  for (const step of renameSteps) {
    const sourceAbsolutePath = path.join(DATASET_ROOT, step.source);
    const targetAbsolutePath = path.join(DATASET_ROOT, step.from);

    if (!fs.existsSync(sourceAbsolutePath)) {
      if (fs.existsSync(targetAbsolutePath)) {
        continue;
      }

      throw new Error(`Cannot rename missing file: ${step.source}`);
    }

    ensureParentDirectory(targetAbsolutePath);

    if (path.resolve(sourceAbsolutePath) === path.resolve(targetAbsolutePath)) {
      continue;
    }

    if (fs.existsSync(targetAbsolutePath)) {
      throw new Error(`Target path already exists: ${step.from}`);
    }

    fs.renameSync(sourceAbsolutePath, targetAbsolutePath);
  }
};

const main = () => {
  const options = parseArgs(process.argv.slice(2));
  validateReviewedPairs();

  const renameMap = buildRenameMap();
  const reviewArtifact = buildReviewArtifact();
  const manifest = buildManifest();

  writeJson(RENAME_MAP_PATH, renameMap);
  writeJson(CURATION_REVIEW_PATH, reviewArtifact);

  if (options.apply) {
    applyApprovedRenames();
  }

  writeJson(MANIFEST_PATH, manifest);

  const approvedCount = manifest.products.length;
  console.log(`Prepared curated dataset artifacts for ${approvedCount} approved products.`);
  if (options.apply) {
    console.log('Applied approved dataset renames.');
  } else {
    console.log('Dry preparation only: rename-map and manifest were updated without renaming files.');
  }
};

main();
