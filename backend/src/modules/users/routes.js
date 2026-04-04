const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { updateProfileValidation } = require('./validation');

const router = express.Router();

router.get('/me', authenticate, asyncHandler(controller.getMyProfile));
router.patch('/me', authenticate, validateRequest(updateProfileValidation), asyncHandler(controller.updateMyProfile));

module.exports = router;
