const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { syncUserValidation } = require('./validation');

const router = express.Router();

router.post('/sync', authenticate, validateRequest(syncUserValidation), asyncHandler(controller.syncUser));
router.get('/me', authenticate, asyncHandler(controller.getMe));

module.exports = router;
