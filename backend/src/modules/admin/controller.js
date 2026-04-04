const { successResponse } = require('../../utils/responseFormatter');
const adminService = require('./service');

const getPendingVendors = async (req, res) => {
  const vendors = await adminService.getPendingVendors();
  return successResponse(res, vendors, 'Pending vendors fetched successfully');
};

const approveVendor = async (req, res) => {
  const vendor = await adminService.approveVendor(req.user.uid, Number(req.params.vendorUserId), req.body.notes);
  return successResponse(res, vendor, 'Vendor approved successfully');
};

const rejectVendor = async (req, res) => {
  const vendor = await adminService.rejectVendor(req.user.uid, Number(req.params.vendorUserId), req.body.reason);
  return successResponse(res, vendor, 'Vendor rejected successfully');
};

const getPendingProducts = async (req, res) => {
  const products = await adminService.getPendingProducts();
  return successResponse(res, products, 'Pending products fetched successfully');
};

const approveProduct = async (req, res) => {
  const product = await adminService.approveProduct(req.user.uid, Number(req.params.productId), req.body.notes);
  return successResponse(res, product, 'Product approved successfully');
};

const rejectProduct = async (req, res) => {
  const product = await adminService.rejectProduct(req.user.uid, Number(req.params.productId), req.body.reason);
  return successResponse(res, product, 'Product rejected successfully');
};

const getReports = async (req, res) => {
  const reports = await adminService.getReports();
  return successResponse(res, reports, 'Reports fetched successfully');
};

module.exports = {
  getPendingVendors,
  approveVendor,
  rejectVendor,
  getPendingProducts,
  approveProduct,
  rejectProduct,
  getReports
};
