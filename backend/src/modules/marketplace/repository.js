const { query } = require("../../config/database");

const searchApprovedProducts = async (filters) => {
  const values = [];
  const clauses = [`p.status = 'approved'`];

  if (filters.search) {
    values.push(`%${filters.search}%`);
    clauses.push(
      `(p.name ILIKE $${values.length} OR p.description ILIKE $${values.length})`,
    );
  }

  if (filters.categoryId) {
    values.push(filters.categoryId);
    clauses.push(`p.category_id = $${values.length}`);
  }

  if (filters.brandName) {
    values.push(filters.brandName);
    clauses.push(`LOWER(p.brand_name) = LOWER($${values.length})`);
  }

  if (filters.minPrice !== undefined) {
    values.push(filters.minPrice);
    clauses.push(`p.price >= $${values.length}`);
  }

  if (filters.maxPrice !== undefined) {
    values.push(filters.maxPrice);
    clauses.push(`p.price <= $${values.length}`);
  }

  values.push(filters.limit);
  values.push(filters.offset);

  const result = await query(
    `
      SELECT
        p.id,
        p.name,
        p.description,
        p.brand_name AS "brandName",
        p.price,
        p.dimensions_json AS dimensions,
        c.id AS "categoryId",
        c.name AS "categoryName",
        u.display_name AS "vendorName",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_key END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_key END)
        ) AS "thumbnailKey",
        COALESCE(
          MAX(CASE WHEN pa.asset_type = 'thumbnail' THEN pa.s3_url END),
          MAX(CASE WHEN pa.asset_type = 'image' THEN pa.s3_url END)
        ) AS "thumbnailUrl"
        ,
        COUNT(*) FILTER (WHERE pa.asset_type = 'model')::INTEGER AS "modelsCount",
        COUNT(*) FILTER (WHERE pa.asset_type = 'texture')::INTEGER AS "texturesCount"
      FROM products p
      LEFT JOIN categories c ON c.id = p.category_id
      INNER JOIN users u ON u.id = p.vendor_id
      LEFT JOIN product_assets pa ON pa.product_id = p.id
      WHERE ${clauses.join(" AND ")}
      GROUP BY p.id, c.id, u.display_name
      ORDER BY p.created_at DESC
      LIMIT $${values.length - 1} OFFSET $${values.length}
    `,
    values,
  );

  return result.rows;
};

const countApprovedProducts = async (filters) => {
  const values = [];
  const clauses = [`status = 'approved'`];

  if (filters.search) {
    values.push(`%${filters.search}%`);
    clauses.push(
      `(name ILIKE $${values.length} OR description ILIKE $${values.length})`,
    );
  }

  if (filters.categoryId) {
    values.push(filters.categoryId);
    clauses.push(`category_id = $${values.length}`);
  }

  if (filters.brandName) {
    values.push(filters.brandName);
    clauses.push(`LOWER(brand_name) = LOWER($${values.length})`);
  }

  if (filters.minPrice !== undefined) {
    values.push(filters.minPrice);
    clauses.push(`price >= $${values.length}`);
  }

  if (filters.maxPrice !== undefined) {
    values.push(filters.maxPrice);
    clauses.push(`price <= $${values.length}`);
  }

  const result = await query(
    `SELECT COUNT(*)::INTEGER AS total FROM products WHERE ${clauses.join(" AND ")}`,
    values,
  );
  return result.rows[0].total;
};

const getApprovedProductDetails = async (productId) => {
  const result = await query(
    `
      SELECT
        p.id,
        p.name,
        p.description,
        p.brand_name AS "brandName",
        p.price,
        p.dimensions_json AS dimensions,
        p.status,
        c.id AS "categoryId",
        c.name AS "categoryName",
        c.slug AS "categorySlug",
        u.id AS "vendorId",
        u.display_name AS "vendorName"
      FROM products p
      LEFT JOIN categories c ON c.id = p.category_id
      INNER JOIN users u ON u.id = p.vendor_id
      WHERE p.id = $1 AND p.status = 'approved'
    `,
    [productId],
  );

  return result.rows[0] || null;
};

const getApprovedBrands = async () => {
  const result = await query(
    `
      SELECT DISTINCT brand_name AS "brandName"
      FROM products
      WHERE status = 'approved'
        AND brand_name IS NOT NULL
        AND BTRIM(brand_name) <> ''
      ORDER BY brand_name ASC
    `,
  );

  return result.rows.map((row) => row.brandName);
};

const getProductAssets = async (productId) => {
  const result = await query(
    `
      SELECT
        id,
        asset_type AS "assetType",
        s3_key AS "s3Key",
        s3_url AS "s3Url",
        file_name AS "fileName",
        mime_type AS "mimeType",
        metadata_json AS metadata
      FROM product_assets
      WHERE product_id = $1
      ORDER BY uploaded_at DESC
    `,
    [productId],
  );

  return result.rows;
};

module.exports = {
  searchApprovedProducts,
  countApprovedProducts,
  getApprovedProductDetails,
  getApprovedBrands,
  getProductAssets,
};
