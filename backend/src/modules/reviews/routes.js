const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { createReviewValidation, productReviewValidation } = require('./validation');

const router = express.Router();

router.post('/', authenticate, validateRequest(createReviewValidation), asyncHandler(controller.createReview));
router.get('/products/:productId', validateRequest(productReviewValidation), asyncHandler(controller.getProductReviews));

module.exports = router;
