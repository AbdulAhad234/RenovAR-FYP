const { query } = require('../../config/database');

const createApprovalLog = async (payload) => {
  const result = await query(
    `
      INSERT INTO admin_approvals (admin_id, resource_type, resource_id, action, notes)
      VALUES ($1, $2, $3, $4, $5)
      RETURNING
        id,
        admin_id AS "adminId",
        resource_type AS "resourceType",
        resource_id AS "resourceId",
        action,
        notes,
        created_at AS "createdAt"
    `,
    [payload.adminId, payload.resourceType, payload.resourceId, payload.action, payload.notes || null]
  );

  return result.rows[0];
};

module.exports = {
  createApprovalLog
};
