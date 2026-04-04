const assetRepository = require('./repository');
const productService = require('../products/service');
const { generateS3Key, uploadToS3 } = require('../../utils/s3Helper');
const { ApiError } = require('../../utils/apiError');

const uploadAsset = async (productId, firebaseUid, payload) => {
  await productService.assertProductOwnership(productId, firebaseUid);

  const fileBuffer = Buffer.from(payload.fileContentBase64, 'base64');
  if (!fileBuffer.length) {
    throw new ApiError(400, 'fileContentBase64 must contain a valid base64 payload', 'BAD_REQUEST');
  }

  const key = generateS3Key(String(productId), payload.assetType, payload.fileName);
  const uploadResult = await uploadToS3(fileBuffer, key, payload.mimeType);

  return assetRepository.createAsset({
    productId,
    assetType: payload.assetType,
    s3Key: uploadResult.key,
    s3Url: uploadResult.location,
    fileName: payload.fileName,
    fileSize: payload.fileSize || fileBuffer.length,
    mimeType: payload.mimeType,
    metadata: payload.metadata
  });
};

const getProductAssets = async (productId, firebaseUid) => {
  await productService.assertProductOwnership(productId, firebaseUid);
  return assetRepository.findByProductId(productId);
};

module.exports = {
  uploadAsset,
  getProductAssets
};
