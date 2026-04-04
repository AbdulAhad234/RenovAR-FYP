const { param } = require('express-validator');

const arProductIdValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

module.exports = {
  arProductIdValidation
};
