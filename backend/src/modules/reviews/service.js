const reviewRepository = require('./repository');
const userRepository = require('../users/repository');
const marketplaceRepository = require('../marketplace/repository');
const { ApiError } = require('../../utils/apiError');

const createReview = async (firebaseUid, payload) => {
  const user = await userRepository.findByFirebaseUid(firebaseUid);
  if (!user) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  const product = await marketplaceRepository.getApprovedProductDetails(payload.productId);
  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  return reviewRepository.createReview({
    productId: payload.productId,
    userId: user.id,
    rating: payload.rating,
    comment: payload.comment,
    isVerifiedPurchase: false
  });
};

const getProductReviews = async (productId) => reviewRepository.getProductReviews(productId);

module.exports = {
  createReview,
  getProductReviews
};
