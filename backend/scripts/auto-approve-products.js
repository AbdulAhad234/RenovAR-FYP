#!/usr/bin/env node

const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { query, pool } = require("../src/config/database");

const DATASET_VENDOR_EMAILS = [
  "urban.living@renovar.local",
  "heritage.home@renovar.local",
  "minimal.nest@renovar.local",
  "wallcraft.surfaces@renovar.local",
];

const parseArgs = (argv) => {
  return {
    vendorEmail: argv.includes("--vendor")
      ? argv[argv.indexOf("--vendor") + 1]
      : null,
    datasetVendors: argv.includes("--dataset-vendors"),
    dryRun: argv.includes("--dry-run"),
  };
};

const findVendorByEmail = async (email) => {
  const result = await query(
    `SELECT id FROM users WHERE LOWER(email) = LOWER($1) LIMIT 1`,
    [email],
  );
  return result.rows[0] || null;
};

const approveVendorProducts = async (vendorEmail, opts) => {
  const vendor = await findVendorByEmail(vendorEmail);
  if (!vendor) throw new Error(`Vendor not found: ${vendorEmail}`);

  const productsRes = await query(
    `SELECT id FROM products WHERE vendor_id = $1 AND status <> 'approved'`,
    [vendor.id],
  );
  const products = productsRes.rows;
  if (products.length === 0) {
    console.log(`No products to approve for vendor ${vendorEmail}.`);
    return;
  }

  console.log(
    `Found ${products.length} products to approve for vendor ${vendorEmail}`,
  );
  if (opts.dryRun) return;

  for (const p of products) {
    await query(
      `UPDATE products SET status = 'approved', reviewed_at = NOW() WHERE id = $1`,
      [p.id],
    );
    await query(
      `INSERT INTO admin_approvals (admin_id, resource_type, resource_id, action, created_at) VALUES ($1,$2,$3,$4,NOW())`,
      [1, "product", p.id, "approve"],
    ).catch(() => {});
  }

  console.log(`Products approved for ${vendorEmail}.`);
};

const run = async () => {
  const opts = parseArgs(process.argv.slice(2));
  const vendorEmails = opts.datasetVendors
    ? DATASET_VENDOR_EMAILS
    : [opts.vendorEmail].filter(Boolean);

  if (vendorEmails.length === 0) {
    throw new Error("Provide --vendor <email> or --dataset-vendors");
  }

  for (const vendorEmail of vendorEmails) {
    await approveVendorProducts(vendorEmail, opts);
  }
};

run()
  .catch((err) => {
    console.error(err.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
