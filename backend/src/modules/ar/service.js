const arRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');

const getArProduct = async (productId) => {
  const product = await arRepository.getArReadyProduct(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  return {
    id: product.id,
    name: product.name,
    price: product.price,
    modelUrl: product.modelUrl,
    thumbnailUrl: product.thumbnailUrl,
    dimensions: product.dimensions,
    metadata: {
      description: product.description
    }
  };
};

module.exports = {
  getArProduct
};
