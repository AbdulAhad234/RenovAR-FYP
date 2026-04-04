const { body, param } = require('express-validator');

const productIdValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

const sharedAssetValidation = [
  ...productIdValidation,
  body('assetType').isIn(['image', 'model', 'thumbnail']),
  body('fileName').isString().trim().isLength({ min: 3, max: 255 }),
  body('mimeType').isString().trim().isLength({ min: 3, max: 100 }),
  body('fileSize').optional().isInt({ min: 1 }),
  body('metadata').optional().isObject()
];

const createUploadSessionValidation = [...sharedAssetValidation];

const confirmAssetValidation = [
  ...sharedAssetValidation,
  body('s3Key').isString().trim().isLength({ min: 10, max: 500 }),
  body('uploadToken').isString().trim().isLength({ min: 20 }),
  body('expiresAt').isISO8601().withMessage('expiresAt must be a valid ISO timestamp')
];

const uploadAssetValidation = [
  ...sharedAssetValidation,
  body('fileContentBase64').isString().trim().isLength({ min: 10 })
];

module.exports = {
  productIdValidation,
  createUploadSessionValidation,
  confirmAssetValidation,
  uploadAssetValidation
};
