const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { requireVendor, requireVendorOrAdmin } = require('../../middlewares/authorization');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { vendorProfileValidation, updateVendorProfileValidation } = require('./validation');

const router = express.Router();

router.post('/profile', authenticate, validateRequest(vendorProfileValidation), asyncHandler(controller.createVendorProfile));
router.patch('/profile', authenticate, requireVendorOrAdmin, validateRequest(updateVendorProfileValidation), asyncHandler(controller.updateVendorProfile));
router.get('/dashboard', authenticate, requireVendor, asyncHandler(controller.getVendorDashboard));

module.exports = router;
