const { successResponse } = require('../../utils/responseFormatter');
const productService = require('./service');

const createProduct = async (req, res) => {
  const product = await productService.createProduct(req.user.uid, req.body);
  return successResponse(res, product, 'Product created successfully', 201);
};

const updateProduct = async (req, res) => {
  const product = await productService.updateProduct(Number(req.params.productId), req.user.uid, req.body);
  return successResponse(res, product, 'Product updated successfully');
};

const deleteProduct = async (req, res) => {
  const result = await productService.deleteProduct(Number(req.params.productId), req.user.uid);
  return successResponse(res, result, 'Product deleted successfully');
};

const getVendorProducts = async (req, res) => {
  const products = await productService.getVendorProducts(req.user.uid);
  return successResponse(res, products, 'Vendor products fetched successfully');
};

const submitProductForApproval = async (req, res) => {
  const product = await productService.submitProductForApproval(Number(req.params.productId), req.user.uid);
  return successResponse(res, product, 'Product submitted for approval successfully');
};

module.exports = {
  createProduct,
  updateProduct,
  deleteProduct,
  getVendorProducts,
  submitProductForApproval
};
