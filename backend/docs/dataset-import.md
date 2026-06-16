# Curated Dataset Import

This backend imports curated 3D model products from the local dataset folder:

- `../3D_Models_Dataset`

## Files

- Curated manifest template: `<DATASET_ROOT>/curated-manifest.example.json` (use your dataset root)
- Working manifest: `<DATASET_ROOT>/curated-manifest.json`
- Rename traceability map: `<DATASET_ROOT>/rename-map.json`
- Review summary: `<DATASET_ROOT>/curation-review.json`
- Dataset vendor seed script: `scripts/seed-dataset-vendors.js`
- Dataset preparation script: `scripts/prepare-curated-dataset.js`
- Import script: `scripts/import-curated-dataset.js`
- Asset repair script: `scripts/fix-assets-from-manifest.js`
- Import wall textures: `scripts/import-wall-textures.js`
- Sync wall texture material maps: `scripts/sync-wall-texture-material-maps.js`
- Product copy update script: `scripts/update-product-copy-from-manifest.js`

## Manifest rules

Each product entry must explicitly define:

- `name`
- `sourceCategory`
- `normalizedCategorySlug`
- `modelPath`
- `primaryImagePath`
- `price`
- `currency` (`PKR` for the current dataset)

Optional fields:

- `importKey`
- `vendorEmail` (falls back to round-robin assignment from top-level `datasetVendors`)
- `brandName` (falls back to top-level `defaultBrandName`, then the vendor company/display name)
- `additionalImagePaths`
- `sourceModelPath`
- `sourcePrimaryImagePath`
- `sourceAdditionalImagePaths`
- `confidence`
- `description`
- `textureTarget` (`floor`, `wall`, or `both`; required for texture products that need AR surface classification)
- `dimensions`
- `tags`

All file paths in the manifest are relative to `3D_Models_Dataset/`.

The top-level `datasetVendors` array defines the four seeded dataset vendors. When a product does not specify `vendorEmail`, the importer assigns products round-robin across those vendors so each vendor owns a different subset of products.

Wall texture entries are written under `wallTextures`. Each entry pairs:

- `textureFolderPath`: the source folder containing all available texture maps
- `textureMapPath`: the single base-color/albedo image the AR app should apply to walls
- `materialMaps`: role-based texture map paths such as `baseColor`, `normal`, `roughness`, `ambientOcclusion`, `metallic`, `height`, `glossiness`, or `reflection`
- `thumbnailPath`: the matching UI preview image
- `textureTarget`: currently `wall` for curated wall textures

## Preparation workflow

Generate the rename map, curation review artifact, and refreshed manifest.

Generic:

```bash
# Regenerates manifest and review artifacts from the dataset folder
npm run prepare:dataset
```

To overwrite or apply changes in-place (if your npm script supports it):

```bash
# Apply prepared changes back to manifest/artifacts (script-specific)
npm run prepare:dataset:apply
```

The preparation step does not rename files. Unmatched images and models remain untouched and are recorded in `rename-map.json` with `status: "unmatched"`.

## Category normalization

Current default mapping:

- `Accessories` -> `decor`
- `Beds` -> `bedroom`
- `Chairs` -> `living-room`
- `Sofas` -> `living-room`
- `Tables` -> `dining-room`

The manifest can override these defaults.

## Import commands

Create or update dataset vendors, then import. Use the dry-run variants first to verify.

Generic workflow (recommended order):

```bash
# 1) Ensure environment and DB ready (set up .env, run migrations)
npm run db:migrate

# 2) Seed dataset vendor accounts (creates vendor users used by imports)
npm run seed:dataset-vendors

# 3) Prepare dataset artifacts (rename map, curation review, manifest)
npm run prepare:dataset

# 4) Dry-run import to validate manifest and file availability
npm run import:dataset:dry-run

# 5) Real import (when dry-run succeeds)
npm run import:dataset

# 6) Wall-texture-specific dry-run / import
npm run import:wall-textures:dry-run
npm run import:wall-textures

# 7) Sync missing material maps (dry-run then real)
npm run sync:wall-texture-maps:dry-run
npm run sync:wall-texture-maps

# 8) Update product copy or metadata after import
npm run update:product-copy

# 9) Approve imported products (all dataset vendors or a single vendor)
npm run approve:vendor-products -- --dataset-vendors
# or for a single vendor
npm run approve:vendor-products -- --vendor <VENDOR_EMAIL>

# 10) Advanced: import with a custom manifest path
# (replace <MANIFEST_PATH> with a relative or absolute path)
node scripts/import-curated-dataset.js --manifest <MANIFEST_PATH> --skip-duplicates
```

## Preconditions

- backend `.env` must be configured
- PostgreSQL schema must already exist
- run `npm run db:migrate` before importing if the database was created from an older schema
- run `npm run seed:dataset-vendors` before importing when using `datasetVendors`
- S3 credentials must be valid
- every vendor in `datasetVendors` must exist in PostgreSQL with role `vendor`

## Import behavior

- products are created as `draft`
- furniture products are distributed round-robin across `datasetVendors` unless `vendorEmail` is set on an entry
- product `brand_name` is populated from `brandName`, `defaultBrandName`, or the vendor company/display name
- product prices are stored as PKR numeric values
- product descriptions come from `scripts/product-copy.js`
- wall texture products are stored with `texture_target = 'wall'`
- model files are stored as `model` assets
- primary image files are stored as `thumbnail` assets
- every supported wall texture material map is stored as a `texture` asset with `metadata_json.mapRole`
- additional images are stored as `image` assets
- product and asset metadata are persisted in PostgreSQL
- original dataset paths and curation confidence are preserved in asset metadata when provided in the manifest
- re-runs can safely skip existing products when `--skip-duplicates` is used

## Repair existing S3/DB asset pairs

If products already exist but their S3 model/thumbnail pair is wrong, first confirm the local pairing file:

```bash
npm run fix:dataset-assets:plan
```

Preview the DB/S3 repair:

```bash
npm run fix:dataset-assets:dry-run
```

Replace the current curated model and thumbnail assets with the files from `curated-manifest.json`:

```bash
npm run fix:dataset-assets
```

By default, the repair deletes matching old `model` and `thumbnail` S3 objects and their `product_assets` rows before uploading the corrected pair. Use `--thumbnails-only` or `--models-only` for a narrower repair, and use `--keep-old` only when you intentionally want duplicate assets.
