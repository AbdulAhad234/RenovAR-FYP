const arRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');
const { generateSignedUrl } = require('../../utils/s3Helper');
const { getPlacementClassification } = require('./placementClassification');
const { DEFAULT_CURRENCY, DEFAULT_CURRENCY_SYMBOL } = require('../../constants/currency');

const normalizeMapRole = (role) => {
  const value = String(role || '').trim();
  if (!value) return null;

  const normalized = value.toLowerCase();
  if (normalized === 'basecolor' || normalized === 'albedo' || normalized === 'diffuse') {
    return 'baseColor';
  }
  if (normalized === 'ao') return 'ambientOcclusion';
  if (normalized === 'nrm') return 'normal';
  if (normalized === 'disp' || normalized === 'bump') return 'height';
  if (normalized === 'gloss') return 'glossiness';
  if (normalized === 'refl') return 'reflection';

  return value;
};

const buildSignedMaterialMaps = async (textureAssets = []) => {
  const materialMaps = {};

  for (const asset of textureAssets) {
    const role = normalizeMapRole(asset.metadata?.mapRole);
    if (!role || !asset.s3Key) {
      continue;
    }

    materialMaps[role] = await generateSignedUrl(asset.s3Key);
  }

  return materialMaps;
};

const getArProduct = async (productId) => {
  const product = await arRepository.getArReadyProduct(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  const modelUrl = product.modelKey ? await generateSignedUrl(product.modelKey) : product.modelUrl;
  const materialMaps = await buildSignedMaterialMaps(product.textureAssets);
  const textureUrl = materialMaps.baseColor || (product.textureKey ? await generateSignedUrl(product.textureKey) : product.textureUrl);
  const thumbnailUrl = product.thumbnailKey ? await generateSignedUrl(product.thumbnailKey) : product.thumbnailUrl;
  const placement = getPlacementClassification(product);

  return {
    id: product.id,
    name: product.name,
    price: product.price,
    currency: DEFAULT_CURRENCY,
    currencySymbol: DEFAULT_CURRENCY_SYMBOL,
    modelUrl,
    textureUrl,
    materialMaps,
    thumbnailUrl,
    dimensions: product.dimensions,
    textureTarget: product.textureTarget,
    arPlacement: placement,
    metadata: {
      description: product.description,
      materialMaps,
      arPlacement: placement
    }
  };
};

module.exports = {
  getArProduct
};
