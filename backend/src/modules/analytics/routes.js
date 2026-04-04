const express = require('express');
const controller = require('./controller');
const { asyncHandler } = require('../../middlewares/errorHandler');

const router = express.Router();

router.get('/placeholder', asyncHandler(controller.getAnalyticsPlaceholder));

module.exports = router;
