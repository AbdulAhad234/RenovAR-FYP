#!/usr/bin/env node

const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { query, pool } = require("../src/config/database");

const DATASET_VENDORS = [
  {
    firebaseUid: "dataset-vendor-urban-living-studio",
    email: "urban.living@renovar.local",
    displayName: "Urban Living Studio",
    companyName: "Urban Living Studio",
    description: "Curated modern furniture and apartment-friendly AR products.",
  },
  {
    firebaseUid: "dataset-vendor-heritage-home-co",
    email: "heritage.home@renovar.local",
    displayName: "Heritage Home Co",
    companyName: "Heritage Home Co",
    description: "Warm wood furniture, classic pieces, and decor for family homes.",
  },
  {
    firebaseUid: "dataset-vendor-minimal-nest",
    email: "minimal.nest@renovar.local",
    displayName: "Minimal Nest",
    companyName: "Minimal Nest",
    description: "Minimal bedroom, living room, and storage products for AR previews.",
  },
  {
    firebaseUid: "dataset-vendor-wallcraft-surfaces",
    email: "wallcraft.surfaces@renovar.local",
    displayName: "Wallcraft Surfaces",
    companyName: "Wallcraft Surfaces",
    description: "Wall textures and surface finishes prepared for RenovAR placement.",
  },
];

const upsertVendorUser = async (vendor) => {
  const result = await query(
    `
      INSERT INTO users (
        firebase_uid,
        email,
        display_name,
        role,
        email_verified
      )
      VALUES ($1, $2, $3, 'vendor', true)
      ON CONFLICT (firebase_uid)
      DO UPDATE SET
        email = EXCLUDED.email,
        display_name = EXCLUDED.display_name,
        role = 'vendor',
        email_verified = true
      RETURNING id, email, display_name AS "displayName"
    `,
    [vendor.firebaseUid, vendor.email, vendor.displayName],
  );

  return result.rows[0];
};

const upsertVendorProfile = async (userId, vendor) => {
  const result = await query(
    `
      INSERT INTO vendor_profiles (
        user_id,
        company_name,
        description,
        status,
        reviewed_at
      )
      VALUES ($1, $2, $3, 'approved', CURRENT_TIMESTAMP)
      ON CONFLICT (user_id)
      DO UPDATE SET
        company_name = EXCLUDED.company_name,
        description = EXCLUDED.description,
        status = 'approved',
        rejection_reason = NULL,
        reviewed_at = CURRENT_TIMESTAMP
      RETURNING id, company_name AS "companyName", status
    `,
    [userId, vendor.companyName, vendor.description],
  );

  return result.rows[0];
};

const main = async () => {
  for (const vendor of DATASET_VENDORS) {
    const user = await upsertVendorUser(vendor);
    const profile = await upsertVendorProfile(user.id, vendor);
    console.log(
      `[SEEDED] ${profile.companyName} (${user.email}) user #${user.id}, profile #${profile.id}`,
    );
  }

  console.log(`Seeded ${DATASET_VENDORS.length} approved dataset vendors.`);
};

main()
  .catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
