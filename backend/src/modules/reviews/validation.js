const { body, param } = require('express-validator');

const createReviewValidation = [
  body('productId').isInt({ min: 1 }),
  body('rating').isInt({ min: 1, max: 5 }),
  body('comment').optional().isString().trim().isLength({ min: 3, max: 500 })
];

const productReviewValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

module.exports = {
  createReviewValidation,
  productReviewValidation
};
