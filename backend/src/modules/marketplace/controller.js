const { successResponse } = require('../../utils/responseFormatter');
const marketplaceService = require('./service');

const searchProducts = async (req, res) => {
  const result = await marketplaceService.searchProducts(req.query);
  return successResponse(res, result, 'Marketplace products fetched successfully');
};

const getProductDetails = async (req, res) => {
  const product = await marketplaceService.getProductDetails(Number(req.params.productId));
  return successResponse(res, product, 'Marketplace product fetched successfully');
};

const getBrands = async (req, res) => {
  const brands = await marketplaceService.getBrands();
  return successResponse(res, brands, 'Marketplace brands fetched successfully');
};

module.exports = {
  searchProducts,
  getProductDetails,
  getBrands
};
