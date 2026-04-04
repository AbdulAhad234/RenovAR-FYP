const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { createReportValidation } = require('./validation');

const router = express.Router();

router.post('/', authenticate, validateRequest(createReportValidation), asyncHandler(controller.createReport));

module.exports = router;
