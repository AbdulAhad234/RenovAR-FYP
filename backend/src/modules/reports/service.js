const reportRepository = require('./repository');
const userRepository = require('../users/repository');
const { ApiError } = require('../../utils/apiError');

const createReport = async (firebaseUid, payload) => {
  const reporter = await userRepository.findByFirebaseUid(firebaseUid);
  if (!reporter) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  return reportRepository.createReport({
    reporterId: reporter.id,
    reportedUserId: payload.reportedUserId,
    contentType: payload.contentType,
    contentId: payload.contentId,
    reason: payload.reason
  });
};

module.exports = {
  createReport
};
