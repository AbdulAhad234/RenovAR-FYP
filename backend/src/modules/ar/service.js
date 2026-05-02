const arRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');
const { generateSignedUrl } = require('../../utils/s3Helper');
const { getPlacementClassification } = require('./placementClassification');
const { DEFAULT_CURRENCY, DEFAULT_CURRENCY_SYMBOL } = require('../../constants/currency');

const getArProduct = async (productId) => {
  const product = await arRepository.getArReadyProduct(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  const modelUrl = product.modelKey ? await generateSignedUrl(product.modelKey) : product.modelUrl;
  const thumbnailUrl = product.thumbnailKey ? await generateSignedUrl(product.thumbnailKey) : product.thumbnailUrl;
  const placement = getPlacementClassification(product);

  return {
    id: product.id,
    name: product.name,
    price: product.price,
    currency: DEFAULT_CURRENCY,
    currencySymbol: DEFAULT_CURRENCY_SYMBOL,
    modelUrl,
    thumbnailUrl,
    dimensions: product.dimensions,
    arPlacement: placement,
    metadata: {
      description: product.description,
      arPlacement: placement
    }
  };
};

module.exports = {
  getArProduct
};
