# Curated Dataset Import

This backend imports curated 3D model products from the local dataset folder:

- `../3D_Models_Dataset`

## Files

- Curated manifest template: `../3D_Models_Dataset/curated-manifest.example.json`
- Working manifest: `../3D_Models_Dataset/curated-manifest.json`
- Rename traceability map: `../3D_Models_Dataset/rename-map.json`
- Review summary: `../3D_Models_Dataset/curation-review.json`
- Dataset preparation script: `scripts/prepare-curated-dataset.js`
- Import script: `scripts/import-curated-dataset.js`
- Asset repair script: `scripts/fix-assets-from-manifest.js`

## Manifest rules

Each product entry must explicitly define:

- `name`
- `sourceCategory`
- `normalizedCategorySlug`
- `modelPath`
- `primaryImagePath`
- `price`

Optional fields:

- `importKey`
- `additionalImagePaths`
- `sourceModelPath`
- `sourcePrimaryImagePath`
- `sourceAdditionalImagePaths`
- `confidence`
- `description`
- `dimensions`
- `tags`

All file paths in the manifest are relative to `3D_Models_Dataset/`.

## Preparation workflow

Generate the rename map, curation review artifact, and refreshed manifest:

```bash
npm run prepare:dataset
```

Apply the approved renames for the curated subset and then rewrite the manifest:

```bash
npm run prepare:dataset:apply
```

The preparation step only renames files for the reviewed curated subset. Unmatched images and models remain untouched and are recorded in `rename-map.json` with `status: "unmatched"`.

## Category normalization

Current default mapping:

- `Accessories` -> `decor`
- `Beds` -> `bedroom`
- `Chairs` -> `living-room`
- `Sofas` -> `living-room`
- `Tables` -> `dining-room`
- `Coffee tables` -> `living-room`

The manifest can override these defaults.

## Import commands

Dry run:

```bash
npm run import:dataset:dry-run
```

Real import:

```bash
npm run import:dataset
```

Custom manifest:

```bash
node scripts/import-curated-dataset.js --manifest ../3D_Models_Dataset/curated-manifest.json --skip-duplicates
```

## Preconditions

- backend `.env` must be configured
- PostgreSQL schema must already exist
- S3 credentials must be valid
- the vendor user in `defaultVendorEmail` must already exist in PostgreSQL with role `vendor`

## Import behavior

- products are created as `draft`
- model files are stored as `model` assets
- primary image files are stored as `thumbnail` assets
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
