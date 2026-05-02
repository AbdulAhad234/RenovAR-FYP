const fs = require("fs");
const path = require("path");
const { Pool } = require("pg");

const MIGRATIONS_DIR = path.join(
  __dirname,
  "..",
  "src",
  "database",
  "migrations",
);

const getMigrations = () => {
  if (!fs.existsSync(MIGRATIONS_DIR)) return [];
  return fs
    .readdirSync(MIGRATIONS_DIR)
    .filter((f) => f.endsWith(".sql"))
    .sort()
    .map((f) => path.join(MIGRATIONS_DIR, f));
};

const run = async () => {
  const databaseUrl = process.env.DATABASE_URL;
  if (!databaseUrl) {
    console.error("DATABASE_URL environment variable is required");
    process.exit(1);
  }

  const pool = new Pool({
    connectionString: databaseUrl,
    ssl: { rejectUnauthorized: false },
  });
  const client = await pool.connect();

  try {
    const migrations = getMigrations();
    if (!migrations.length) {
      console.log("No migration files found.");
      return;
    }

    for (const m of migrations) {
      console.log("Applying migration", m);
      const sql = fs.readFileSync(m, "utf8");
      try {
        await client.query("BEGIN");
        await client.query(sql);
        await client.query("COMMIT");
        console.log("Applied:", m);
      } catch (err) {
        await client.query("ROLLBACK");
        console.error("Failed applying migration", m, err.message || err);
        throw err;
      }
    }

    console.log("Migrations applied successfully");
  } catch (err) {
    console.error("Migration process failed:", err.message || err);
    process.exit(1);
  } finally {
    client.release();
    await pool.end();
  }
};

run();
