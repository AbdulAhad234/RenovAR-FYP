const { query } = require('../../config/database');

const createReview = async (payload) => {
  const result = await query(
    `
      INSERT INTO reviews (product_id, user_id, rating, comment, is_verified_purchase)
      VALUES ($1, $2, $3, $4, $5)
      RETURNING
        id,
        product_id AS "productId",
        user_id AS "userId",
        rating,
        comment,
        is_verified_purchase AS "isVerifiedPurchase",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [payload.productId, payload.userId, payload.rating, payload.comment || null, payload.isVerifiedPurchase || false]
  );

  return result.rows[0];
};

const getProductReviews = async (productId) => {
  const result = await query(
    `
      SELECT
        r.id,
        r.product_id AS "productId",
        r.user_id AS "userId",
        u.display_name AS "userName",
        r.rating,
        r.comment,
        r.is_verified_purchase AS "isVerifiedPurchase",
        r.created_at AS "createdAt",
        r.updated_at AS "updatedAt"
      FROM reviews r
      INNER JOIN users u ON u.id = r.user_id
      WHERE r.product_id = $1
      ORDER BY r.created_at DESC
    `,
    [productId]
  );

  return result.rows;
};

module.exports = {
  createReview,
  getProductReviews
};
