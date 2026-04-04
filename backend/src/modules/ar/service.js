const arRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');
const { generateSignedUrl } = require('../../utils/s3Helper');

const getArProduct = async (productId) => {
  const product = await arRepository.getArReadyProduct(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  const modelUrl = product.modelKey ? await generateSignedUrl(product.modelKey) : product.modelUrl;
  const thumbnailUrl = product.thumbnailKey ? await generateSignedUrl(product.thumbnailKey) : product.thumbnailUrl;

  return {
    id: product.id,
    name: product.name,
    price: product.price,
    modelUrl,
    thumbnailUrl,
    dimensions: product.dimensions,
    metadata: {
      description: product.description
    }
  };
};

module.exports = {
  getArProduct
};
