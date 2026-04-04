const { successResponse } = require('../../utils/responseFormatter');
const assetService = require('./service');

const uploadAsset = async (req, res) => {
  const asset = await assetService.uploadAsset(Number(req.params.productId), req.user.uid, req.body);
  return successResponse(res, asset, 'Asset uploaded successfully', 201);
};

const getProductAssets = async (req, res) => {
  const assets = await assetService.getProductAssets(Number(req.params.productId), req.user.uid);
  return successResponse(res, assets, 'Product assets fetched successfully');
};

module.exports = {
  uploadAsset,
  getProductAssets
};
