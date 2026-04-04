const { successResponse } = require('../../utils/responseFormatter');
const reportService = require('./service');

const createReport = async (req, res) => {
  const report = await reportService.createReport(req.user.uid, req.body);
  return successResponse(res, report, 'Report submitted successfully', 201);
};

module.exports = {
  createReport
};
