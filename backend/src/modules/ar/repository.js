const { query } = require('../../config/database');

const getArReadyProduct = async (productId) => {
  const result = await query(
    `
      SELECT
        p.id,
        p.name,
        p.price,
        p.dimensions_json AS dimensions,
        p.description,
        MAX(CASE WHEN pa.asset_type = 'model' THEN pa.s3_key END) AS "modelKey",
        MAX(CASE WHEN pa.asset_type = 'model' THEN pa.s3_url END) AS "modelUrl",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_key END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_key END)
        ) AS "thumbnailKey",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_url END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_url END)
        ) AS "thumbnailUrl"
      FROM products p
      LEFT JOIN product_assets pa ON pa.product_id = p.id
      WHERE p.id = $1 AND p.status = 'approved'
      GROUP BY p.id
    `,
    [productId]
  );

  return result.rows[0] || null;
};

module.exports = {
  getArReadyProduct
};
