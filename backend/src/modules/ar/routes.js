const express = require('express');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { arProductIdValidation } = require('./validation');

const router = express.Router();

router.get('/products/:productId', validateRequest(arProductIdValidation), asyncHandler(controller.getArProduct));

module.exports = router;
