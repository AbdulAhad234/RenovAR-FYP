const { query } = require("../../config/database");

const createAsset = async (payload) => {
  const result = await query(
    `
      INSERT INTO product_assets (
        product_id,
        asset_type,
        s3_key,
        s3_url,
        file_name,
        file_size,
        mime_type,
        metadata_json
      )
      VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
      RETURNING
        id,
        product_id AS "productId",
        asset_type AS "assetType",
        s3_key AS "s3Key",
        s3_url AS "s3Url",
        file_name AS "fileName",
        file_size AS "fileSize",
        mime_type AS "mimeType",
        metadata_json AS metadata,
        uploaded_at AS "uploadedAt"
    `,
    [
      payload.productId,
      payload.assetType,
      payload.s3Key,
      payload.s3Url,
      payload.fileName,
      payload.fileSize,
      payload.mimeType,
      payload.metadata || null,
    ],
  );

  return result.rows[0];
};

const findByProductIdAndS3Key = async (productId, s3Key) => {
  const result = await query(
    `
      SELECT
        id,
        product_id AS "productId",
        asset_type AS "assetType",
        s3_key AS "s3Key",
        s3_url AS "s3Url",
        file_name AS "fileName",
        file_size AS "fileSize",
        mime_type AS "mimeType",
        metadata_json AS metadata,
        uploaded_at AS "uploadedAt"
      FROM product_assets
      WHERE product_id = $1 AND s3_key = $2
      LIMIT 1
    `,
    [productId, s3Key],
  );

  return result.rows[0] || null;
};

const findByProductId = async (productId) => {
  const result = await query(
    `
      SELECT
        id,
        product_id AS "productId",
        asset_type AS "assetType",
        s3_key AS "s3Key",
        s3_url AS "s3Url",
        file_name AS "fileName",
        file_size AS "fileSize",
        mime_type AS "mimeType",
        metadata_json AS metadata,
        uploaded_at AS "uploadedAt"
      FROM product_assets
      WHERE product_id = $1
      ORDER BY uploaded_at DESC
    `,
    [productId],
  );

  return result.rows;
};

const getTexturesByProductIdAndMapRole = async (productId, mapRole) => {
  const result = await query(
    `
      SELECT
        id,
        product_id AS "productId",
        asset_type AS "assetType",
        s3_key AS "s3Key",
        s3_url AS "s3Url",
        file_name AS "fileName",
        file_size AS "fileSize",
        mime_type AS "mimeType",
        metadata_json AS metadata,
        uploaded_at AS "uploadedAt"
      FROM product_assets
      WHERE product_id = $1
        AND asset_type = 'texture'
        AND (metadata_json->>'mapRole') = $2
      ORDER BY uploaded_at ASC
    `,
    [productId, mapRole],
  );

  return result.rows;
};

const deleteAssetById = async (id) => {
  const result = await query(
    `
      DELETE FROM product_assets
      WHERE id = $1
      RETURNING s3_key AS "s3Key"
    `,
    [id],
  );

  return result.rows[0] || null;
};

module.exports = {
  createAsset,
  findByProductId,
  findByProductIdAndS3Key,
  getTexturesByProductIdAndMapRole,
  deleteAssetById,
};
