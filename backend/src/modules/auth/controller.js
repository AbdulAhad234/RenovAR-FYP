const { successResponse } = require('../../utils/responseFormatter');
const authService = require('./service');

const syncUser = async (req, res) => {
  const user = await authService.syncAuthenticatedUser(req.user, req.body.role);
  return successResponse(res, user, 'User synchronized successfully');
};

const getMe = async (req, res) => {
  const user = await authService.getCurrentUser(req.user.uid);
  return successResponse(res, user, 'Authenticated user fetched successfully');
};

module.exports = {
  syncUser,
  getMe
};
