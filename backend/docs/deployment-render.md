# Render Deployment Guide

## Deployment target

- Platform: Render
- Service type: Web Service
- Root directory: `backend`

## Build settings

- Build command: `npm install`
- Start command: `npm start`

## Required environment variables

- `NODE_ENV=production`
- `DATABASE_URL`
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

## Post-deploy workflow

- Deploy backend API first.
- Verify DB, Firebase, Firestore, and S3 connectivity.
- Run curated dataset import separately after the API is healthy.
