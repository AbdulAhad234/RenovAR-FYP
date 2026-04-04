const { successResponse } = require('../../utils/responseFormatter');
const vendorService = require('./service');

const createVendorProfile = async (req, res) => {
  const vendor = await vendorService.createVendorProfile(req.user.uid, req.body);
  return successResponse(res, vendor, 'Vendor profile created successfully', 201);
};

const updateVendorProfile = async (req, res) => {
  const vendor = await vendorService.updateVendorProfile(req.user.uid, req.body);
  return successResponse(res, vendor, 'Vendor profile updated successfully');
};

const getVendorDashboard = async (req, res) => {
  const dashboard = await vendorService.getVendorDashboard(req.user.uid);
  return successResponse(res, dashboard, 'Vendor dashboard fetched successfully');
};

module.exports = {
  createVendorProfile,
  updateVendorProfile,
  getVendorDashboard
};
