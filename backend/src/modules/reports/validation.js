const { body } = require('express-validator');

const createReportValidation = [
  body('reportedUserId').optional().isInt({ min: 1 }),
  body('contentType').isIn(['user', 'product', 'review']),
  body('contentId').isInt({ min: 1 }),
  body('reason').isString().trim().isLength({ min: 10, max: 1000 })
];

module.exports = {
  createReportValidation
};
