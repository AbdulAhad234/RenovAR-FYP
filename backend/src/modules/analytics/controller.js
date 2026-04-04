const { successResponse } = require('../../utils/responseFormatter');

const getAnalyticsPlaceholder = async (req, res) => {
  return successResponse(
    res,
    {
      message: 'Analytics module scaffolded for future implementation'
    },
    'Analytics placeholder ready'
  );
};

module.exports = {
  getAnalyticsPlaceholder
};
