const express = require('express');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { searchMarketplaceValidation, marketplaceProductIdValidation } = require('./validation');

const router = express.Router();

router.get('/products', validateRequest(searchMarketplaceValidation), asyncHandler(controller.searchProducts));
router.get('/brands', asyncHandler(controller.getBrands));
router.get('/products/:productId', validateRequest(marketplaceProductIdValidation), asyncHandler(controller.getProductDetails));

module.exports = router;
