const { successResponse } = require('../../utils/responseFormatter');
const assetService = require('./service');

const createUploadSession = async (req, res) => {
  const session = await assetService.createUploadSession(Number(req.params.productId), req.user.uid, req.body);
  return successResponse(res, session, 'Upload session created successfully');
};

const confirmAssetUpload = async (req, res) => {
  const asset = await assetService.confirmAssetUpload(Number(req.params.productId), req.user.uid, req.body);
  return successResponse(res, asset, 'Asset registered successfully', 201);
};

const uploadAsset = async (req, res) => {
  const asset = await assetService.uploadAsset(Number(req.params.productId), req.user.uid, req.body);
  return successResponse(res, asset, 'Asset uploaded successfully', 201);
};

const getProductAssets = async (req, res) => {
  const assets = await assetService.getProductAssets(Number(req.params.productId), req.user.uid);
  return successResponse(res, assets, 'Product assets fetched successfully');
};

module.exports = {
  createUploadSession,
  confirmAssetUpload,
  uploadAsset,
  getProductAssets
};
