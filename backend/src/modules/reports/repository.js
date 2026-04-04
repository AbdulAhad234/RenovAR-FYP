const { query } = require('../../config/database');

const createReport = async (payload) => {
  const result = await query(
    `
      INSERT INTO reports (reporter_id, reported_user_id, content_type, content_id, reason, status)
      VALUES ($1, $2, $3, $4, $5, 'pending')
      RETURNING
        id,
        reporter_id AS "reporterId",
        reported_user_id AS "reportedUserId",
        content_type AS "contentType",
        content_id AS "contentId",
        reason,
        status,
        admin_notes AS "adminNotes",
        resolved_by AS "resolvedBy",
        resolved_at AS "resolvedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [payload.reporterId, payload.reportedUserId || null, payload.contentType, payload.contentId, payload.reason]
  );

  return result.rows[0];
};

const getReports = async () => {
  const result = await query(
    `
      SELECT
        id,
        reporter_id AS "reporterId",
        reported_user_id AS "reportedUserId",
        content_type AS "contentType",
        content_id AS "contentId",
        reason,
        status,
        admin_notes AS "adminNotes",
        resolved_by AS "resolvedBy",
        resolved_at AS "resolvedAt",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
      FROM reports
      ORDER BY created_at DESC
    `
  );

  return result.rows;
};

module.exports = {
  createReport,
  getReports
};
