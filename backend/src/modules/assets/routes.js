const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { requireVendor } = require('../../middlewares/authorization');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { productIdValidation, uploadAssetValidation } = require('./validation');

const router = express.Router();

router.post('/products/:productId', authenticate, requireVendor, validateRequest(uploadAssetValidation), asyncHandler(controller.uploadAsset));
router.get('/products/:productId', authenticate, requireVendor, validateRequest(productIdValidation), asyncHandler(controller.getProductAssets));

module.exports = router;
