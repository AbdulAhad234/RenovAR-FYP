const userRepository = require('../users/repository');
const vendorRepository = require('../vendors/repository');
const productRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');

const getVendorUser = async (firebaseUid) => {
  const user = await userRepository.findByFirebaseUid(firebaseUid);

  if (!user) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  const vendorProfile = await vendorRepository.findByUserId(user.id);
  if (!vendorProfile) {
    throw new ApiError(403, 'Vendor profile is required before product management', 'FORBIDDEN');
  }

  return { user, vendorProfile };
};

const assertProductOwnership = async (productId, firebaseUid) => {
  const { user } = await getVendorUser(firebaseUid);
  const product = await productRepository.findById(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  if (product.vendorId !== user.id) {
    throw new ApiError(403, 'You do not own this product', 'PRODUCT_NOT_OWNED');
  }

  return { user, product };
};

const createProduct = async (firebaseUid, payload) => {
  const { user, vendorProfile } = await getVendorUser(firebaseUid);

  if (vendorProfile.status !== 'approved') {
    throw new ApiError(403, 'Vendor account is not approved yet', 'VENDOR_NOT_APPROVED');
  }

  return productRepository.createProduct({
    vendorId: user.id,
    categoryId: payload.categoryId,
    name: payload.name,
    brandName: payload.brandName || vendorProfile.companyName,
    description: payload.description,
    textureTarget: payload.textureTarget,
    price: payload.price,
    dimensions: payload.dimensions
  });
};

const updateProduct = async (productId, firebaseUid, payload) => {
  await assertProductOwnership(productId, firebaseUid);

  return productRepository.updateProduct(productId, {
    categoryId: payload.categoryId,
    name: payload.name,
    brandName: payload.brandName,
    description: payload.description,
    textureTarget: payload.textureTarget,
    price: payload.price,
    dimensions: payload.dimensions,
    status: payload.status,
    rejectionReason: payload.rejectionReason
  });
};

const deleteProduct = async (productId, firebaseUid) => {
  await assertProductOwnership(productId, firebaseUid);
  await productRepository.deleteProduct(productId);
  return { productId };
};

const getVendorProducts = async (firebaseUid) => {
  const { user } = await getVendorUser(firebaseUid);
  return productRepository.findByVendorId(user.id);
};

const submitProductForApproval = async (productId, firebaseUid) => {
  const { product } = await assertProductOwnership(productId, firebaseUid);
  const assetSummary = await productRepository.countAssetsByProductId(productId);

  if (!assetSummary.models) {
    throw new ApiError(400, 'A 3D model is required before submitting a product', 'BAD_REQUEST');
  }

  return productRepository.updateProduct(productId, {
    categoryId: product.categoryId,
    name: product.name,
    brandName: product.brandName,
    description: product.description,
    textureTarget: product.textureTarget,
    price: product.price,
    dimensions: product.dimensions,
    status: 'pending',
    rejectionReason: null
  });
};

module.exports = {
  createProduct,
  updateProduct,
  deleteProduct,
  getVendorProducts,
  submitProductForApproval,
  assertProductOwnership
};
