# Curated Dataset Import

This backend imports curated 3D model products from the local dataset folder:

- `../3D_Models_Dataset`

## Files

- Curated manifest template: `../3D_Models_Dataset/curated-manifest.example.json`
- Working manifest: `../3D_Models_Dataset/curated-manifest.json`
- Rename traceability map: `../3D_Models_Dataset/rename-map.json`
- Review summary: `../3D_Models_Dataset/curation-review.json`
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

Generate the rename map, curation review artifact, and refreshed manifest:

```bash
npm run prepare:dataset
```

Apply the same preparation script and rewrite the manifest artifacts:

```bash
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

Create or update the four approved dataset vendors:

```bash
npm run seed:dataset-vendors
```

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

Wall texture dry run:

```bash
npm run import:wall-textures:dry-run
```

Wall texture import:

```bash
npm run import:wall-textures
```

Upload missing material maps for wall texture products that already exist:

```bash
npm run sync:wall-texture-maps:dry-run
npm run sync:wall-texture-maps
```

Update descriptions and texture targets for products that were already imported:

```bash
npm run update:product-copy
```

Approve all imported products for the configured vendor:

```bash
npm run approve:vendor-products -- --dataset-vendors
```

Approve one vendor only:

```bash
npm run approve:vendor-products -- --vendor renovar.test1@gmail.com
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
