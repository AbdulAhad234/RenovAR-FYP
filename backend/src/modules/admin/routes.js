const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { requireAdmin } = require('../../middlewares/authorization');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { vendorActionValidation, productActionValidation } = require('./validation');

const router = express.Router();

router.use(authenticate, requireAdmin);

router.get('/vendors/pending', asyncHandler(controller.getPendingVendors));
router.post('/vendors/:vendorUserId/approve', validateRequest(vendorActionValidation), asyncHandler(controller.approveVendor));
router.post('/vendors/:vendorUserId/reject', validateRequest(vendorActionValidation), asyncHandler(controller.rejectVendor));
router.get('/products/pending', asyncHandler(controller.getPendingProducts));
router.post('/products/:productId/approve', validateRequest(productActionValidation), asyncHandler(controller.approveProduct));
router.post('/products/:productId/reject', validateRequest(productActionValidation), asyncHandler(controller.rejectProduct));
router.get('/reports', asyncHandler(controller.getReports));

module.exports = router;
