# RenovAR

RenovAR is an AR-based interior design and furniture marketplace. The project now lives in one repository with the Node/Express backend, the Unity AR frontend, and the supporting 3D dataset kept together under the same RenovAR folder.

## Repository Layout

```text
RenovAR/
+-- backend/                 # Node.js API, database scripts, API docs, deployment notes
+-- frontend/                # Unity frontend imported from RenovAR-Unity-Frontend
|   +-- Augmented reality/   # Actual Unity project folder to open in Unity Hub
+-- 3D_Models_Dataset/       # Curated model and texture dataset manifests/assets
+-- render.yaml              # Render deployment config
+-- README.md                # Single project guide for frontend and backend
```

## What The App Does

RenovAR lets users browse furniture and surface products, view marketplace details, keep a local cart, and preview selected items in augmented reality. The backend manages authentication sync, vendors, products, assets, marketplace browsing, AR-ready product payloads, templates, reviews, reports, and admin approval workflows.

## Tech Stack

Backend:

- Node.js 18+
- Express.js REST API
- Firebase Authentication and Firebase Admin SDK
- Firestore for template documents
- PostgreSQL for marketplace and business data
- AWS S3 for product assets
- Jest and ESLint for testing/linting

Frontend:

- Unity project named `RenovAR`
- AR Foundation 6.1.1
- ARCore and ARKit providers
- XR Interaction Toolkit
- glTFast for loading 3D assets
- Firebase Unity SDK assets, compiled behind the `USE_FIREBASE` scripting symbol
- TextMesh Pro and Unity UI

## Main Features

User app:

- Login and signup screens
- Firebase-backed auth flow, with mock auth available for local frontend development
- Marketplace catalogue loaded from the backend
- Product thumbnails and AR product data cached on device
- Product model and texture placement scenes
- Local cart UI
- AR object placement, manipulation, selection, and visualization helpers

Backend API:

- `POST /api/auth/sync` and `GET /api/auth/me`
- User profile endpoints
- Vendor profile and dashboard endpoints
- Product create/update/delete/submit endpoints
- S3 asset upload and asset listing endpoints
- Marketplace product listing, brand filtering, and product detail endpoints
- `GET /api/ar/products/:productId` for Unity AR payloads
- Firestore-backed template CRUD and sharing
- Reviews, reports, and admin approval endpoints

## Prerequisites

Install these before running the full project:

- Node.js 18 or newer
- npm
- PostgreSQL, or a hosted PostgreSQL database such as Render Postgres
- Firebase project with Authentication enabled
- Firebase Admin service account credentials for the backend
- AWS S3 bucket and credentials for asset storage
- Unity Editor compatible with the project packages
- Android build support for ARCore testing, or iOS build support for ARKit testing

## Backend Setup

From the repository root:

```powershell
cd backend
npm install
Copy-Item .env.example .env
```

Fill `backend/.env` with your real service values:

```env
NODE_ENV=development
PORT=3000
DATABASE_URL=postgresql://username:password@localhost:5432/renovar_db
DB_SSL=false
FIREBASE_PROJECT_ID=your-firebase-project-id
FIREBASE_PRIVATE_KEY="-----BEGIN PRIVATE KEY-----\nYOUR_PRIVATE_KEY_HERE\n-----END PRIVATE KEY-----\n"
FIREBASE_CLIENT_EMAIL=firebase-adminsdk-xxxxx@your-project.iam.gserviceaccount.com
GOOGLE_APPLICATION_CREDENTIALS=./path/to/service-account.json
AWS_ACCESS_KEY_ID=your-aws-access-key-id
AWS_SECRET_ACCESS_KEY=your-aws-secret-access-key
AWS_REGION=us-east-1
AWS_S3_BUCKET_NAME=your-renovar-bucket
ASSET_UPLOAD_SIGNING_SECRET=your-random-long-signing-secret
ALLOWED_ORIGINS=http://localhost:3000
```

For a fresh database, apply the schema and migrations:

```powershell
npm run db:schema
npm run db:migrate
```

Start the API in development mode:

```powershell
npm run dev
```

The backend defaults to port `3000`. Health and docs endpoints are available at:

- `GET http://localhost:3000/health`
- `GET http://localhost:3000/docs`

Production mode:

```powershell
npm start
```

## Dataset Setup

The dataset scripts expect the curated manifest under `3D_Models_Dataset/curated-manifest.json`.

Recommended fresh setup:

```powershell
cd backend
npm run db:schema
npm run db:migrate
npm run seed:dataset-vendors
npm run import:dataset:dry-run
npm run import:dataset
npm run import:wall-textures:dry-run
npm run import:wall-textures
npm run approve:vendor-products -- --dataset-vendors
```

Useful maintenance commands:

```powershell
npm run prepare:dataset
npm run prepare:dataset:apply
npm run fix:dataset-assets:plan
npm run fix:dataset-assets:dry-run
npm run fix:dataset-assets
npm run import:floor-textures:dry-run
npm run import:floor-textures
```

More detail is in `backend/docs/dataset-import.md` and `backend/docs/deployment-render.md`.

## Frontend Setup

Open the Unity project folder in Unity Hub:

```text
D:\Programming Projects\RenovAR\frontend\Augmented reality
```

When Unity opens the project, it will restore packages from `frontend/Augmented reality/Packages/manifest.json`. Important packages include AR Foundation, ARCore, ARKit, XR Interaction Toolkit, glTFast, and Unity UI.

Enabled build scenes are configured in `ProjectSettings/EditorBuildSettings.asset`:

- `SplashScene`
- `LoginScene`
- `SignUpScene`
- `HomepageScene`
- `ProductModelScene`
- `AddToCartScene`
- `ProductTextureScene`

`ProductCatalogue` is present but currently disabled in build settings.

## Connecting Unity To The Backend

The Unity frontend currently points at the deployed backend URL:

```text
https://renovar-backend-8fg8.onrender.com/api
```

This value appears in:

- `frontend/Augmented reality/Assets/Scripts/Managers/APIManager.cs`
- `frontend/Augmented reality/Assets/Scripts/Managers/AuthManager.cs`

For local backend testing, set both `backendBaseUrl` values in the Unity Inspector or scripts to:

```text
http://localhost:3000/api
```

For Android device testing against a backend running on your PC, use your computer's LAN IP address instead of `localhost`, for example:

```text
http://192.168.1.25:3000/api
```

Make sure your phone and development machine are on the same network and that the backend port is reachable through the firewall.

## Authentication Modes

The frontend `AuthManager` has `useMockAuth = true` by default. In mock mode, Unity can move through the login flow without Firebase being compiled and initialized.

For real Firebase login:

1. Add the Firebase Unity configuration files required for your target platform.
2. Ensure the Firebase Unity SDK assets are present and imported correctly.
3. Add the `USE_FIREBASE` scripting define symbol in Unity Player Settings.
4. Set `useMockAuth` to false on the `AuthManager` object if you want Firebase failures to block login.
5. Confirm the backend Firebase Admin credentials in `backend/.env` match the same Firebase project.

After a successful Firebase login or signup, Unity calls `POST /api/auth/sync` with the Firebase ID token. The backend verifies the token and creates or updates the matching PostgreSQL user.

## Typical Development Workflow

1. Start PostgreSQL and any required local services.
2. Run the backend:

```powershell
cd backend
npm run dev
```

3. Open `frontend/Augmented reality` in Unity Hub.
4. Confirm `APIManager.backendBaseUrl` and `AuthManager.backendBaseUrl` point to the backend you want to use.
5. Press Play in Unity for editor testing, or build to an ARCore/ARKit device for real AR placement.
6. Seed/import/approve backend products when the catalogue is empty.

## Backend Scripts

```powershell
npm run dev                         # Run backend with nodemon
npm start                           # Run backend with node
npm test                            # Run Jest tests
npm run lint                        # Run ESLint
npm run db:schema                   # Apply full schema
npm run db:migrate                  # Run migration script
npm run seed:dataset-vendors        # Create dataset vendors
npm run import:dataset              # Import curated 3D products
npm run import:wall-textures        # Import wall textures
npm run import:floor-textures       # Import floor textures
npm run approve:vendor-products -- --dataset-vendors
```

## Deployment Notes

The backend can be deployed on Render using the included `render.yaml`. Render free PostgreSQL databases can expire, so the recovery path is:

1. Create a new database.
2. Replace `DATABASE_URL` in the backend environment.
3. Run `npm run db:schema` and `npm run db:migrate`.
4. Seed dataset vendors.
5. Re-import and approve dataset products.

For production, keep Firebase credentials, AWS credentials, and database URLs out of Git. Use Render environment variables or your host's secret manager.

## Project Notes

- `backend/.env` and other local env files are ignored by Git.
- Unity-generated folders such as `Library`, `Temp`, `Obj`, `Build`, `Builds`, `Logs`, and `UserSettings` should not be committed.
- Firebase custom claims control vendor/admin access. Refresh user tokens after changing roles.
- Product brand filtering is supported through `brandName` and `GET /api/marketplace/brands`.
- The frontend caches marketplace products, thumbnails, and AR product payloads on device for faster repeat loading.

## Current Remaining Work

- Complete full endpoint-by-endpoint manual API testing.
- Add automated backend tests for critical auth, vendor, product, marketplace, template, and admin flows.
- Test the Unity app against both local and deployed backend environments.
- Replace mock auth with Firebase mode for production demos.
- Improve uploads beyond base64 JSON if large assets are uploaded directly from clients.
- Add production hardening such as rate limiting, security headers, stronger input validation, structured logging, and a migration system.

