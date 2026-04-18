# Curated Dataset Import

This backend imports curated 3D model products from the local dataset folder:

- `../3D_Models_Dataset`

## Files

- Curated manifest template: `../3D_Models_Dataset/curated-manifest.example.json`
- Working manifest: `../3D_Models_Dataset/curated-manifest.json`
- Import script: `scripts/import-curated-dataset.js`

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
- `description`
- `dimensions`
- `tags`

All file paths in the manifest are relative to `3D_Models_Dataset/`.

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
- re-runs can safely skip existing products when `--skip-duplicates` is used
