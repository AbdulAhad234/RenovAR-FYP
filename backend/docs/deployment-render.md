# Render Deployment Guide

Render Free PostgreSQL databases currently expire after 30 days, have a 1 GB limit, and do not include backups. A workspace can have one active Free PostgreSQL database at a time. If you are staying on the free tier, create a replacement database, point the backend `DATABASE_URL` to it, run the schema/migrations, and re-import the dataset.

## Deployment target

- Platform: Render
- Service type: Web Service
- Root directory: `backend`

## Build settings

- Build command: `npm install`
- Start command: `npm start`

## Required environment variables

- `NODE_ENV=production`
- `DATABASE_URL` from the active PostgreSQL database
- `DB_SSL=true` or `PGSSLMODE=require` when connecting to a hosted PostgreSQL database that requires SSL
- `FIREBASE_PROJECT_ID`
- `FIREBASE_PRIVATE_KEY_ID`
- `FIREBASE_PRIVATE_KEY`
- `FIREBASE_CLIENT_EMAIL`
- `FIREBASE_CLIENT_ID`
- `FIREBASE_AUTH_URI`
- `FIREBASE_TOKEN_URI`
- `FIREBASE_AUTH_PROVIDER_X509_CERT_URL`
- `FIREBASE_CLIENT_X509_CERT_URL`
- `GOOGLE_APPLICATION_CREDENTIALS` only if you rely on a service-account file path
- `AWS_ACCESS_KEY_ID`
- `AWS_SECRET_ACCESS_KEY`
- `AWS_REGION`
- `AWS_S3_BUCKET_NAME`
- `ASSET_UPLOAD_SIGNING_SECRET`
- `ALLOWED_ORIGINS`
- `LOG_LEVEL`

## Production checklist

1. Create a managed PostgreSQL database.
2. Apply the RenovAR schema before first production use.
3. Create the Render web service with root directory `backend`.
4. Set all required environment variables.
5. Confirm `/health` works after deployment.
6. Confirm Firebase token verification works in production.
7. Confirm signed S3 upload and signed read URLs work in production.

## Replacing an expired Render PostgreSQL database

1. In Render, create a new PostgreSQL database on the free plan.
2. If Render says you already have one active free database, wait until the expired one is no longer active or delete the old free database if you no longer need it.
3. Open the database **Info** page and copy the **Internal Database URL** if the backend web service is also on Render and in the same region. Use the **External Database URL** only for local terminal access from your laptop.
4. Open the Render web service for `renovar-backend`.
5. Go to **Environment** and replace `DATABASE_URL` with the new database URL.
6. Keep `NODE_ENV=production`; set `DB_SSL=true` if the connection requires SSL.
7. Redeploy the backend service.
8. From your local `backend` folder, temporarily point `DATABASE_URL` at the new database external URL and run the setup commands below.
9. Seed the dataset vendors, re-import the dataset, and approve products again if the new database starts empty.

## PostgreSQL setup commands

Run these from `backend/`.

Apply the full schema to a new empty database:

```bash
npm run db:schema
```

If PostgreSQL CLI tools are installed, this works too:

```bash
psql "$DATABASE_URL" -f src/database/schema.sql
```

PowerShell equivalent:

```powershell
psql $env:DATABASE_URL -f src/database/schema.sql
```

Apply migrations, including product brand support:

```bash
npm run db:migrate
```

Seed the four approved dataset vendors:

```bash
npm run seed:dataset-vendors
```

If you cannot run `psql`, paste the contents of `src/database/schema.sql` into the Render PostgreSQL shell first, then run the SQL files in `src/database/migrations/` in filename order.

Useful checks after setup:

```sql
SELECT COUNT(*) AS categories FROM categories;
SELECT column_name FROM information_schema.columns WHERE table_name = 'products' AND column_name = 'brand_name';
SELECT id, name, brand_name, status FROM products ORDER BY id DESC LIMIT 10;
SELECT DISTINCT brand_name FROM products WHERE brand_name IS NOT NULL ORDER BY brand_name;
SELECT u.email, vp.company_name, COUNT(p.id) AS products
FROM users u
JOIN vendor_profiles vp ON vp.user_id = u.id
LEFT JOIN products p ON p.vendor_id = u.id
WHERE u.email IN (
  'urban.living@renovar.local',
  'heritage.home@renovar.local',
  'minimal.nest@renovar.local',
  'wallcraft.surfaces@renovar.local'
)
GROUP BY u.email, vp.company_name
ORDER BY u.email;
```

## Post-deploy workflow

- Deploy backend API first.
- Verify DB, Firebase, Firestore, and S3 connectivity.
- Run curated dataset import separately after the API is healthy.
