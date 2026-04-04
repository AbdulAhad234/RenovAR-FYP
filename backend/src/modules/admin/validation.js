const { body, param } = require('express-validator');

const vendorActionValidation = [
  param('vendorUserId').isInt({ min: 1 }),
  body('notes').optional().isString().trim().isLength({ min: 3, max: 500 }),
  body('reason').optional().isString().trim().isLength({ min: 3, max: 500 })
];

const productActionValidation = [
  param('productId').isInt({ min: 1 }),
  body('notes').optional().isString().trim().isLength({ min: 3, max: 500 }),
  body('reason').optional().isString().trim().isLength({ min: 3, max: 500 })
];

module.exports = {
  vendorActionValidation,
  productActionValidation
};
