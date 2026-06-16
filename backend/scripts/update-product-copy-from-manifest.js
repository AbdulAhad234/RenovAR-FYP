#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
require("dotenv").config({ path: path.resolve(__dirname, "../.env") });
const { query, pool } = require("../src/config/database");
const {
  getProductDescription,
  getWallTextureDescription,
} = require("./product-copy");

const DEFAULT_MANIFEST_PATH = path.resolve(
  __dirname,
  "../../3D_Models_Dataset/curated-manifest.json",
);

const parseArgs = (argv) => {
  const args = {
    manifest: DEFAULT_MANIFEST_PATH,
    dryRun: false,
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg === "--manifest") {
      args.manifest = path.resolve(process.cwd(), argv[index + 1]);
      index += 1;
    } else if (arg === "--dry-run") {
      args.dryRun = true;
    }
  }

  return args;
};

const loadManifest = (manifestPath) => {
  if (!fs.existsSync(manifestPath)) {
    throw new Error(`Manifest not found: ${manifestPath}`);
  }

  return JSON.parse(fs.readFileSync(manifestPath, "utf8"));
};

const updateProductByManifestKey = async ({
  importKey,
  description,
  textureTarget,
  dryRun,
}) => {
  const result = await query(
    `
      SELECT DISTINCT p.id, p.name
      FROM products p
      INNER JOIN product_assets pa ON pa.product_id = p.id
      WHERE pa.metadata_json->>'manifestKey' = $1
    `,
    [importKey],
  );

  if (!result.rows.length) {
    return { matched: 0, updated: 0 };
  }

  if (dryRun) {
    console.log(
      `[DRY-RUN] ${importKey}: would update ${result.rows.length} product(s)`,
    );
    return { matched: result.rows.length, updated: 0 };
  }

  await query(
    `
      UPDATE products
      SET
        description = $2,
        texture_target = COALESCE($3, texture_target)
      WHERE id = ANY($1::int[])
    `,
    [result.rows.map((row) => row.id), description, textureTarget || null],
  );

  return { matched: result.rows.length, updated: result.rows.length };
};

const main = async () => {
  const options = parseArgs(process.argv.slice(2));
  const manifest = loadManifest(options.manifest);
  const entries = [
    ...(manifest.products || []).map((entry) => ({
      importKey: entry.importKey,
      description: getProductDescription(entry),
      textureTarget: entry.textureTarget || null,
    })),
    ...(manifest.wallTextures || []).map((entry) => ({
      importKey: entry.importKey,
      description: getWallTextureDescription(entry),
      textureTarget: "wall",
    })),
  ];

  const summary = {
    processed: 0,
    matched: 0,
    updated: 0,
    missing: 0,
  };

  for (const entry of entries) {
    if (!entry.importKey) continue;

    summary.processed += 1;
    const result = await updateProductByManifestKey({
      ...entry,
      dryRun: options.dryRun,
    });
    summary.matched += result.matched;
    summary.updated += result.updated;
    if (!result.matched) {
      summary.missing += 1;
      console.warn(`[MISS] No imported product found for ${entry.importKey}`);
    }
  }

  console.log("\nProduct copy update summary:");
  console.log(JSON.stringify(summary, null, 2));
};

main()
  .catch((error) => {
    console.error(error.message);
    process.exitCode = 1;
  })
  .finally(async () => {
    await pool.end();
  });
