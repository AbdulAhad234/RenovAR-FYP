const vendorRepository = require('../vendors/repository');
const productRepository = require('../products/repository');
const reportRepository = require('../reports/repository');
const adminRepository = require('./repository');
const userRepository = require('../users/repository');
const { setCustomClaims } = require('../../utils/tokenHelper');
const { ApiError } = require('../../utils/apiError');

const getAdminUser = async (firebaseUid) => {
  const admin = await userRepository.findByFirebaseUid(firebaseUid);
  if (!admin) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }
  return admin;
};

const getPendingVendors = async () => vendorRepository.findPendingVendors();

const approveVendor = async (firebaseUid, vendorUserId, notes) => {
  const admin = await getAdminUser(firebaseUid);
  const vendor = await vendorRepository.updateVendorStatus(vendorUserId, {
    status: 'approved',
    rejectionReason: null,
    reviewedBy: admin.id
  });

  if (!vendor) {
    throw new ApiError(404, 'Vendor profile not found', 'VENDOR_NOT_FOUND');
  }

  const vendorUser = await userRepository.findById(vendorUserId);
  if (vendorUser) {
    await setCustomClaims(vendorUser.firebaseUid, { role: 'vendor' });
  }

  await adminRepository.createApprovalLog({
    adminId: admin.id,
    resourceType: 'vendor',
    resourceId: vendorUserId,
    action: 'approve',
    notes
  });

  return vendor;
};

const rejectVendor = async (firebaseUid, vendorUserId, reason) => {
  const admin = await getAdminUser(firebaseUid);
  const vendor = await vendorRepository.updateVendorStatus(vendorUserId, {
    status: 'rejected',
    rejectionReason: reason,
    reviewedBy: admin.id
  });

  if (!vendor) {
    throw new ApiError(404, 'Vendor profile not found', 'VENDOR_NOT_FOUND');
  }

  await adminRepository.createApprovalLog({
    adminId: admin.id,
    resourceType: 'vendor',
    resourceId: vendorUserId,
    action: 'reject',
    notes: reason
  });

  return vendor;
};

const getPendingProducts = async () => productRepository.findPendingProducts();

const approveProduct = async (firebaseUid, productId, notes) => {
  const admin = await getAdminUser(firebaseUid);
  const product = await productRepository.updateReviewState(productId, {
    status: 'approved',
    rejectionReason: null,
    reviewedBy: admin.id
  });

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  await adminRepository.createApprovalLog({
    adminId: admin.id,
    resourceType: 'product',
    resourceId: productId,
    action: 'approve',
    notes
  });

  return product;
};

const rejectProduct = async (firebaseUid, productId, reason) => {
  const admin = await getAdminUser(firebaseUid);
  const product = await productRepository.updateReviewState(productId, {
    status: 'rejected',
    rejectionReason: reason,
    reviewedBy: admin.id
  });

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  await adminRepository.createApprovalLog({
    adminId: admin.id,
    resourceType: 'product',
    resourceId: productId,
    action: 'reject',
    notes: reason
  });

  return product;
};

const getReports = async () => reportRepository.getReports();

module.exports = {
  getPendingVendors,
  approveVendor,
  rejectVendor,
  getPendingProducts,
  approveProduct,
  rejectProduct,
  getReports
};
