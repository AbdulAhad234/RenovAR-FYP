const { body } = require('express-validator');

const vendorProfileValidation = [
  body('companyName').isString().trim().isLength({ min: 2, max: 255 }),
  body('description').optional().isString().trim().isLength({ min: 10, max: 1000 }),
  body('avatarUrl').optional().isURL().withMessage('avatarUrl must be a valid URL')
];

const updateVendorProfileValidation = [
  body('companyName').optional().isString().trim().isLength({ min: 2, max: 255 }),
  body('description').optional().isString().trim().isLength({ min: 10, max: 1000 }),
  body('avatarUrl').optional().isURL().withMessage('avatarUrl must be a valid URL')
];

module.exports = {
  vendorProfileValidation,
  updateVendorProfileValidation
};
