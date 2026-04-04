const { successResponse } = require('../../utils/responseFormatter');
const arService = require('./service');

const getArProduct = async (req, res) => {
  const product = await arService.getArProduct(Number(req.params.productId));
  return successResponse(res, product, 'AR-ready product fetched successfully');
};

module.exports = {
  getArProduct
};
