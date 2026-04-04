const { query } = require('../../config/database');

const productSelect = `
  SELECT
    p.id,
    p.vendor_id AS "vendorId",
    p.category_id AS "categoryId",
    c.name AS "categoryName",
    c.slug AS "categorySlug",
    p.name,
    p.description,
    p.price,
    p.dimensions_json AS dimensions,
    p.status,
    p.rejection_reason AS "rejectionReason",
    p.reviewed_by AS "reviewedBy",
    p.reviewed_at AS "reviewedAt",
    p.created_at AS "createdAt",
    p.updated_at AS "updatedAt"
  FROM products p
  LEFT JOIN categories c ON c.id = p.category_id
`;

const createProduct = async (payload) => {
  const result = await query(
    `
      INSERT INTO products (vendor_id, category_id, name, description, price, dimensions_json, status)
      VALUES ($1, $2, $3, $4, $5, $6, 'draft')
      RETURNING
        id,
        vendor_id AS "vendorId",
        category_id AS "categoryId",
        name,
        description,
        price,
        dimensions_json AS dimensions,
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [payload.vendorId, payload.categoryId || null, payload.name, payload.description || null, payload.price, payload.dimensions || null]
  );

  return result.rows[0];
};

const updateProduct = async (productId, payload) => {
  const result = await query(
    `
      UPDATE products
      SET
        category_id = COALESCE($2, category_id),
        name = COALESCE($3, name),
        description = COALESCE($4, description),
        price = COALESCE($5, price),
        dimensions_json = COALESCE($6, dimensions_json),
        status = COALESCE($7, status),
        rejection_reason = $8
      WHERE id = $1
      RETURNING
        id,
        vendor_id AS "vendorId",
        category_id AS "categoryId",
        name,
        description,
        price,
        dimensions_json AS dimensions,
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [
      productId,
      payload.categoryId,
      payload.name,
      payload.description,
      payload.price,
      payload.dimensions,
      payload.status,
      payload.rejectionReason || null
    ]
  );

  return result.rows[0] || null;
};

const findById = async (productId) => {
  const result = await query(`${productSelect} WHERE p.id = $1`, [productId]);
  return result.rows[0] || null;
};

const deleteProduct = async (productId) => {
  const result = await query('DELETE FROM products WHERE id = $1 RETURNING id', [productId]);
  return result.rows[0] || null;
};

const findByVendorId = async (vendorId) => {
  const result = await query(`${productSelect} WHERE p.vendor_id = $1 ORDER BY p.created_at DESC`, [vendorId]);
  return result.rows;
};

const countAssetsByProductId = async (productId) => {
  const result = await query(
    `
      SELECT
        COUNT(*) FILTER (WHERE asset_type = 'image')::INTEGER AS images,
        COUNT(*) FILTER (WHERE asset_type = 'thumbnail')::INTEGER AS thumbnails,
        COUNT(*) FILTER (WHERE asset_type = 'model')::INTEGER AS models
      FROM product_assets
      WHERE product_id = $1
    `,
    [productId]
  );

  return result.rows[0];
};

const updateReviewState = async (productId, payload) => {
  const result = await query(
    `
      UPDATE products
      SET
        status = $2,
        rejection_reason = $3,
        reviewed_by = $4,
        reviewed_at = CURRENT_TIMESTAMP
      WHERE id = $1
      RETURNING
        id,
        vendor_id AS "vendorId",
        category_id AS "categoryId",
        name,
        description,
        price,
        dimensions_json AS dimensions,
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [productId, payload.status, payload.rejectionReason || null, payload.reviewedBy]
  );

  return result.rows[0] || null;
};

const findPendingProducts = async () => {
  const result = await query(`${productSelect} WHERE p.status = 'pending' ORDER BY p.created_at ASC`);
  return result.rows;
};

module.exports = {
  createProduct,
  updateProduct,
  findById,
  deleteProduct,
  findByVendorId,
  countAssetsByProductId,
  updateReviewState,
  findPendingProducts
};
