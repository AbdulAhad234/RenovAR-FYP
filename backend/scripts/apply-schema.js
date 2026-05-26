#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { Pool } = require("pg");

const SCHEMA_PATH = path.resolve(__dirname, "../src/database/schema.sql");

const shouldUseSsl = () => {
  const databaseUrl = process.env.DATABASE_URL || "";
  const explicitSsl = String(process.env.DB_SSL || "").toLowerCase();
  const sslMode = String(process.env.PGSSLMODE || "").toLowerCase();

  if (explicitSsl === "true" || explicitSsl === "1") return true;
  if (explicitSsl === "false" || explicitSsl === "0") return false;
  if (sslMode === "require" || sslMode === "prefer") return true;

  return databaseUrl.includes("render.com");
};

const main = async () => {
  if (!process.env.DATABASE_URL) {
    throw new Error("DATABASE_URL environment variable is required");
  }

  if (!fs.existsSync(SCHEMA_PATH)) {
    throw new Error(`Schema file not found: ${SCHEMA_PATH}`);
  }

  const pool = new Pool({
    connectionString: process.env.DATABASE_URL,
    ssl: shouldUseSsl() ? { rejectUnauthorized: false } : false,
  });

  try {
    const schemaSql = fs.readFileSync(SCHEMA_PATH, "utf8");
    await pool.query(schemaSql);
    console.log("Database schema applied successfully.");
  } finally {
    await pool.end();
  }
};

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
