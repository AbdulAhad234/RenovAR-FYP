const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { requireVendor } = require('../../middlewares/authorization');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const {
  createProductValidation,
  updateProductValidation,
  productIdParamValidation
} = require('./validation');

const router = express.Router();

router.post('/', authenticate, requireVendor, validateRequest(createProductValidation), asyncHandler(controller.createProduct));
router.patch('/:productId', authenticate, requireVendor, validateRequest(updateProductValidation), asyncHandler(controller.updateProduct));
router.delete('/:productId', authenticate, requireVendor, validateRequest(productIdParamValidation), asyncHandler(controller.deleteProduct));
router.get('/vendor/me', authenticate, requireVendor, asyncHandler(controller.getVendorProducts));
router.post('/:productId/submit', authenticate, requireVendor, validateRequest(productIdParamValidation), asyncHandler(controller.submitProductForApproval));

module.exports = router;
