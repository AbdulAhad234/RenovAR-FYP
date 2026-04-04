const { query } = require('../../config/database');

const userSelect = `
  SELECT
    id,
    firebase_uid AS "firebaseUid",
    email,
    display_name AS "displayName",
    phone,
    address,
    profile_pic_url AS "profilePicUrl",
    role,
    email_verified AS "emailVerified",
    created_at AS "createdAt",
    updated_at AS "updatedAt"
  FROM users
`;

const findByFirebaseUid = async (firebaseUid) => {
  const result = await query(`${userSelect} WHERE firebase_uid = $1`, [firebaseUid]);
  return result.rows[0] || null;
};

const findById = async (id) => {
  const result = await query(`${userSelect} WHERE id = $1`, [id]);
  return result.rows[0] || null;
};

const upsertUser = async (userData) => {
  const result = await query(
    `
      INSERT INTO users (
        firebase_uid,
        email,
        display_name,
        profile_pic_url,
        role,
        email_verified
      )
      VALUES ($1, $2, $3, $4, $5, $6)
      ON CONFLICT (firebase_uid)
      DO UPDATE SET
        email = EXCLUDED.email,
        display_name = COALESCE(EXCLUDED.display_name, users.display_name),
        profile_pic_url = COALESCE(EXCLUDED.profile_pic_url, users.profile_pic_url),
        role = COALESCE(EXCLUDED.role, users.role),
        email_verified = EXCLUDED.email_verified
      RETURNING
        id,
        firebase_uid AS "firebaseUid",
        email,
        display_name AS "displayName",
        phone,
        address,
        profile_pic_url AS "profilePicUrl",
        role,
        email_verified AS "emailVerified",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [
      userData.firebaseUid,
      userData.email,
      userData.displayName || null,
      userData.profilePicUrl || null,
      userData.role,
      userData.emailVerified || false
    ]
  );

  return result.rows[0];
};

const updateUserProfile = async (id, updates) => {
  const result = await query(
    `
      UPDATE users
      SET
        display_name = COALESCE($2, display_name),
        phone = COALESCE($3, phone),
        address = COALESCE($4, address),
        profile_pic_url = COALESCE($5, profile_pic_url)
      WHERE id = $1
      RETURNING
        id,
        firebase_uid AS "firebaseUid",
        email,
        display_name AS "displayName",
        phone,
        address,
        profile_pic_url AS "profilePicUrl",
        role,
        email_verified AS "emailVerified",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [id, updates.displayName, updates.phone, updates.address, updates.profilePicUrl]
  );

  return result.rows[0] || null;
};

const updateUserRole = async (id, role) => {
  const result = await query(
    `
      UPDATE users
      SET role = $2
      WHERE id = $1
      RETURNING
        id,
        firebase_uid AS "firebaseUid",
        email,
        display_name AS "displayName",
        phone,
        address,
        profile_pic_url AS "profilePicUrl",
        role,
        email_verified AS "emailVerified",
        created_at AS "createdAt",
        updated_at AS "updatedAt"
    `,
    [id, role]
  );

  return result.rows[0] || null;
};

module.exports = {
  findByFirebaseUid,
  findById,
  upsertUser,
  updateUserProfile,
  updateUserRole
};
