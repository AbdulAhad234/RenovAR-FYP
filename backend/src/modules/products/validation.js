const { body, param } = require('express-validator');

const productIdParamValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

const createProductValidation = [
  body('categoryId').optional().isInt({ min: 1 }),
  body('name').isString().trim().isLength({ min: 2, max: 255 }),
  body('brandName').optional().isString().trim().isLength({ min: 2, max: 255 }),
  body('description').optional().isString().trim().isLength({ min: 10, max: 1000 }),
  body('price').isFloat({ min: 0 }),
  body('dimensions').optional().isObject().withMessage('dimensions must be an object')
];

const updateProductValidation = [
  ...productIdParamValidation,
  body('categoryId').optional().isInt({ min: 1 }),
  body('name').optional().isString().trim().isLength({ min: 2, max: 255 }),
  body('brandName').optional().isString().trim().isLength({ min: 2, max: 255 }),
  body('description').optional().isString().trim().isLength({ min: 10, max: 1000 }),
  body('price').optional().isFloat({ min: 0 }),
  body('status').optional().isIn(['draft', 'pending', 'approved', 'rejected']),
  body('rejectionReason').optional().isString().trim().isLength({ min: 3, max: 500 }),
  body('dimensions').optional().isObject().withMessage('dimensions must be an object')
];

module.exports = {
  createProductValidation,
  updateProductValidation,
  productIdParamValidation
};
