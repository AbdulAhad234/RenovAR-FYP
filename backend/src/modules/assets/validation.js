const { body, param } = require('express-validator');

const productIdValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

const uploadAssetValidation = [
  ...productIdValidation,
  body('assetType').isIn(['image', 'model', 'thumbnail']),
  body('fileName').isString().trim().isLength({ min: 3, max: 255 }),
  body('mimeType').isString().trim().isLength({ min: 3, max: 100 }),
  body('fileContentBase64').isString().trim().isLength({ min: 10 }),
  body('fileSize').optional().isInt({ min: 1 }),
  body('metadata').optional().isObject()
];

module.exports = {
  productIdValidation,
  uploadAssetValidation
};
