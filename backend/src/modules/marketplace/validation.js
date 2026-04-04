const { param, query } = require('express-validator');

const searchMarketplaceValidation = [
  query('categoryId').optional().isInt({ min: 1 }),
  query('minPrice').optional().isFloat({ min: 0 }),
  query('maxPrice').optional().isFloat({ min: 0 }),
  query('page').optional().isInt({ min: 1 }),
  query('limit').optional().isInt({ min: 1, max: 100 })
];

const marketplaceProductIdValidation = [
  param('productId').isInt({ min: 1 }).withMessage('productId must be a positive integer')
];

module.exports = {
  searchMarketplaceValidation,
  marketplaceProductIdValidation
};
