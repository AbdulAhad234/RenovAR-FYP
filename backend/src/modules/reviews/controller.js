const { successResponse } = require('../../utils/responseFormatter');
const reviewService = require('./service');

const createReview = async (req, res) => {
  const review = await reviewService.createReview(req.user.uid, req.body);
  return successResponse(res, review, 'Review created successfully', 201);
};

const getProductReviews = async (req, res) => {
  const reviews = await reviewService.getProductReviews(Number(req.params.productId));
  return successResponse(res, reviews, 'Product reviews fetched successfully');
};

module.exports = {
  createReview,
  getProductReviews
};
