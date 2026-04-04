const { query } = require('../../config/database');

const vendorSelect = `
  SELECT
    vp.id,
    vp.user_id AS "userId",
    vp.company_name AS "companyName",
    vp.description,
    vp.avatar_url AS "avatarUrl",
    vp.status,
    vp.rejection_reason AS "rejectionReason",
    vp.reviewed_by AS "reviewedBy",
    vp.reviewed_at AS "reviewedAt",
    vp.created_at AS "createdAt",
    vp.updated_at AS "updatedAt",
    u.email,
    u.display_name AS "displayName"
  FROM vendor_profiles vp
  INNER JOIN users u ON u.id = vp.user_id
`;

const findByUserId = async (userId) => {
  const result = await query(`${vendorSelect} WHERE vp.user_id = $1`, [userId]);
  return result.rows[0] || null;
};

const createVendorProfile = async (payload) => {
  const result = await query(
    `
      INSERT INTO vendor_profiles (user_id, company_name, description, avatar_url, status)
      VALUES ($1, $2, $3, $4, 'pending')
      RETURNING
        id,
        user_id AS "userId",
        company_name AS "companyName",
        description,
        avatar_url AS "avatarUrl",
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [payload.userId, payload.companyName, payload.description || null, payload.avatarUrl || null]
  );

  return result.rows[0];
};

const updateVendorProfile = async (userId, payload) => {
  const result = await query(
    `
      UPDATE vendor_profiles
      SET
        company_name = COALESCE($2, company_name),
        description = COALESCE($3, description),
        avatar_url = COALESCE($4, avatar_url)
      WHERE user_id = $1
      RETURNING
        id,
        user_id AS "userId",
        company_name AS "companyName",
        description,
        avatar_url AS "avatarUrl",
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [userId, payload.companyName, payload.description, payload.avatarUrl]
  );

  return result.rows[0] || null;
};

const getVendorDashboard = async (userId) => {
  const result = await query(
    `
      SELECT
        COUNT(*) FILTER (WHERE p.status = 'draft')::INTEGER AS "draftProducts",
        COUNT(*) FILTER (WHERE p.status = 'pending')::INTEGER AS "pendingProducts",
        COUNT(*) FILTER (WHERE p.status = 'approved')::INTEGER AS "approvedProducts",
        COUNT(*) FILTER (WHERE p.status = 'rejected')::INTEGER AS "rejectedProducts",
        COUNT(pa.id)::INTEGER AS "assetCount"
      FROM products p
      LEFT JOIN product_assets pa ON pa.product_id = p.id
      WHERE p.vendor_id = $1
    `,
    [userId]
  );

  return result.rows[0];
};

const updateVendorStatus = async (userId, payload) => {
  const result = await query(
    `
      UPDATE vendor_profiles
      SET
        status = $2,
        rejection_reason = $3,
        reviewed_by = $4,
        reviewed_at = CURRENT_TIMESTAMP
      WHERE user_id = $1
      RETURNING
        id,
        user_id AS "userId",
        company_name AS "companyName",
        description,
        avatar_url AS "avatarUrl",
        status,
        rejection_reason AS "rejectionReason",
        reviewed_by AS "reviewedBy",
        reviewed_at AS "reviewedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [userId, payload.status, payload.rejectionReason || null, payload.reviewedBy]
  );

  return result.rows[0] || null;
};

const findPendingVendors = async () => {
  const result = await query(`${vendorSelect} WHERE vp.status = 'pending' ORDER BY vp.created_at ASC`);
  return result.rows;
};

module.exports = {
  findByUserId,
  createVendorProfile,
  updateVendorProfile,
  getVendorDashboard,
  updateVendorStatus,
  findPendingVendors
};
