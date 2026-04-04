const userRepository = require('../users/repository');
const vendorRepository = require('./repository');
const { setCustomClaims } = require('../../utils/tokenHelper');
const { ApiError } = require('../../utils/apiError');

const getVendorProfileByFirebaseUid = async (firebaseUid) => {
  const user = await userRepository.findByFirebaseUid(firebaseUid);

  if (!user) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  const vendor = await vendorRepository.findByUserId(user.id);

  if (!vendor) {
    throw new ApiError(404, 'Vendor profile not found', 'VENDOR_NOT_FOUND');
  }

  return { user, vendor };
};

const createVendorProfile = async (firebaseUid, payload) => {
  const user = await userRepository.findByFirebaseUid(firebaseUid);

  if (!user) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  const existingVendor = await vendorRepository.findByUserId(user.id);
  if (existingVendor) {
    throw new ApiError(409, 'Vendor profile already exists', 'VENDOR_ALREADY_EXISTS');
  }

  if (user.role !== 'vendor') {
    await userRepository.updateUserRole(user.id, 'vendor');
    await setCustomClaims(firebaseUid, { role: 'vendor' });
  }

  return vendorRepository.createVendorProfile({
    userId: user.id,
    companyName: payload.companyName,
    description: payload.description,
    avatarUrl: payload.avatarUrl
  });
};

const updateVendorProfile = async (firebaseUid, payload) => {
  const { user } = await getVendorProfileByFirebaseUid(firebaseUid);
  return vendorRepository.updateVendorProfile(user.id, payload);
};

const getVendorDashboard = async (firebaseUid) => {
  const { user, vendor } = await getVendorProfileByFirebaseUid(firebaseUid);
  const dashboard = await vendorRepository.getVendorDashboard(user.id);

  return {
    vendor,
    metrics: dashboard
  };
};

module.exports = {
  getVendorProfileByFirebaseUid,
  createVendorProfile,
  updateVendorProfile,
  getVendorDashboard
};
