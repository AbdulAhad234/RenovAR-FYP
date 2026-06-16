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
        p.texture_target AS "textureTarget",
        c.slug AS "categorySlug",
        MAX(CASE WHEN pa.asset_type = 'model' THEN pa.s3_key END) AS "modelKey",
        MAX(CASE WHEN pa.asset_type = 'model' THEN pa.s3_url END) AS "modelUrl",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'texture' AND pa.metadata_json->>'mapRole' = 'baseColor' THEN pa.s3_key END),
          MAX(CASE WHEN pa.asset_type = 'texture' THEN pa.s3_key END)
        ) AS "textureKey",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'texture' AND pa.metadata_json->>'mapRole' = 'baseColor' THEN pa.s3_url END),
          MAX(CASE WHEN pa.asset_type = 'texture' THEN pa.s3_url END)
        ) AS "textureUrl",
        COALESCE(
          JSONB_AGG(
            JSONB_BUILD_OBJECT(
              's3Key', pa.s3_key,
              's3Url', pa.s3_url,
              'fileName', pa.file_name,
              'mimeType', pa.mime_type,
              'metadata', pa.metadata_json
            )
            ORDER BY pa.uploaded_at ASC
          ) FILTER (WHERE pa.asset_type = 'texture'),
          '[]'::jsonb
        ) AS "textureAssets",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_key END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_key END)
        ) AS "thumbnailKey",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_url END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_url END)
        ) AS "thumbnailUrl"
      FROM products p
      LEFT JOIN categories c ON c.id = p.category_id
      LEFT JOIN product_assets pa ON pa.product_id = p.id
      WHERE p.id = $1 AND p.status = 'approved'
      GROUP BY p.id, c.slug
    `,
    [productId]
  );

  return result.rows[0] || null;
};

module.exports = {
  getArReadyProduct
};
