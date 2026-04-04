const { successResponse } = require('../../utils/responseFormatter');
const userService = require('./service');

const getMyProfile = async (req, res) => {
  const profile = await userService.getProfileByFirebaseUid(req.user.uid);
  return successResponse(res, profile, 'User profile fetched successfully');
};

const updateMyProfile = async (req, res) => {
  const profile = await userService.updateProfileByFirebaseUid(req.user.uid, req.body);
  return successResponse(res, profile, 'User profile updated successfully');
};

module.exports = {
  getMyProfile,
  updateMyProfile
};
