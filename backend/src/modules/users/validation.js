const { body } = require('express-validator');

const updateProfileValidation = [
  body('displayName').optional().isString().trim().isLength({ min: 2, max: 100 }),
  body('phone').optional().isString().trim().isLength({ min: 7, max: 20 }),
  body('address').optional().isString().trim().isLength({ min: 5, max: 500 }),
  body('profilePicUrl').optional().isURL().withMessage('profilePicUrl must be a valid URL')
];

module.exports = {
  updateProfileValidation
};
