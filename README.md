# RenovAR Backend

Backend API for RenovAR, an AR-based interior design and furniture marketplace Final Year Project.

## Tech Stack

- Node.js
- Express.js
- Firebase Authentication
- Firestore
- PostgreSQL
- AWS S3
- REST APIs

## Current Status

The backend foundation and the main modular API structure have been implemented.

## What Has Been Done

### 1. Project structure created

The required modular folder structure is in place under [src](D:\Programming Projects\RenovAR\src), including:

- `config`
- `modules`
- `middlewares`
- `utils`
- `constants`
- `database`
- `docs`
- `app.js`
- `server.js`

All requested modules were scaffolded:

- `auth`
- `users`
- `vendors`
- `products`
- `assets`
- `marketplace`
- `ar`
- `templates`
- `reviews`
- `reports`
- `admin`
- `analytics` placeholder

Each main module includes:

- `controller.js`
- `service.js`
- `repository.js`
- `routes.js`
- `validation.js`

### 2. Server and shared backend setup completed

Implemented:

- Express application setup
- centralized error handling
- validation middleware
- authentication middleware
- authorization middleware
- CORS setup
- request logging
- health route
- docs route with sample API payloads

Main files:

- [app.js](D:\Programming Projects\RenovAR\src\app.js)
- [server.js](D:\Programming Projects\RenovAR\src\server.js)
- [errorHandler.js](D:\Programming Projects\RenovAR\src\middlewares\errorHandler.js)
- [validation.js](D:\Programming Projects\RenovAR\src\middlewares\validation.js)
- [authentication.js](D:\Programming Projects\RenovAR\src\middlewares\authentication.js)
- [authorization.js](D:\Programming Projects\RenovAR\src\middlewares\authorization.js)

### 3. Database and service configuration completed

Configured:

- PostgreSQL connection
- Firebase Admin SDK token verification
- Firestore client
- AWS S3 client and helpers

Main files:

- [database.js](D:\Programming Projects\RenovAR\src\config\database.js)
- [firebase.js](D:\Programming Projects\RenovAR\src\config\firebase.js)
- [firestore.js](D:\Programming Projects\RenovAR\src\config\firestore.js)
- [aws.js](D:\Programming Projects\RenovAR\src\config\aws.js)
- [s3Helper.js](D:\Programming Projects\RenovAR\src\utils\s3Helper.js)

### 4. PostgreSQL schema created and applied

Schema includes:

- `users`
- `categories`
- `vendor_profiles`
- `products`
- `product_assets`
- `reviews`
- `reports`
- `admin_approvals`

Schema file:

- [schema.sql](D:\Programming Projects\RenovAR\src\database\schema.sql)

### 5. Implemented API modules

#### Auth module

Implemented:

- verify Firebase token through middleware
- sync authenticated user into PostgreSQL
- get current authenticated backend user

Routes:

- `POST /api/auth/sync`
- `GET /api/auth/me`

#### Users module

Implemented:

- get user profile
- update user profile

Routes:

- `GET /api/users/me`
- `PATCH /api/users/me`

#### Vendors module

Implemented:

- create vendor profile
- update vendor profile
- get vendor dashboard
- vendor role claim support

Routes:

- `POST /api/vendors/profile`
- `PATCH /api/vendors/profile`
- `GET /api/vendors/dashboard`

#### Products module

Implemented:

- create product
- update product
- delete product
- get vendor products
- submit product for approval

Routes:

- `POST /api/products`
- `PATCH /api/products/:productId`
- `DELETE /api/products/:productId`
- `GET /api/products/vendor/me`
- `POST /api/products/:productId/submit`

#### Assets module

Implemented:

- upload asset to S3
- save asset metadata in PostgreSQL
- fetch vendor-owned product assets

Current upload format:

- JSON body with base64 file content

Routes:

- `POST /api/assets/products/:productId`
- `GET /api/assets/products/:productId`

#### Marketplace module

Implemented:

- list approved public products
- search and filter products
- get product details

Routes:

- `GET /api/marketplace/products`
- `GET /api/marketplace/products/:productId`

#### AR module

Implemented:

- lightweight AR-ready product response for Unity/mobile

Routes:

- `GET /api/ar/products/:productId`

#### Templates module

Implemented:

- create template in Firestore
- get user templates
- update template
- delete template
- share template

Routes:

- `POST /api/templates`
- `GET /api/templates/me`
- `PATCH /api/templates/:templateId`
- `DELETE /api/templates/:templateId`
- `POST /api/templates/:templateId/share`

#### Reviews module

Implemented:

- create review
- get product reviews

Routes:

- `POST /api/reviews`
- `GET /api/reviews/products/:productId`

#### Reports module

Implemented:

- create report

Routes:

- `POST /api/reports`

#### Admin module

Implemented:

- get pending vendors
- approve vendor
- reject vendor
- get pending products
- approve product
- reject product
- view reports

Routes:

- `GET /api/admin/vendors/pending`
- `POST /api/admin/vendors/:vendorUserId/approve`
- `POST /api/admin/vendors/:vendorUserId/reject`
- `GET /api/admin/products/pending`
- `POST /api/admin/products/:productId/approve`
- `POST /api/admin/products/:productId/reject`
- `GET /api/admin/reports`

### 6. Example API documentation added

Example request/response structures are available at:

- `GET /docs`
- [apiExamples.js](D:\Programming Projects\RenovAR\src\docs\apiExamples.js)

### 7. Initial manual testing completed

Successfully tested:

- backend boot
- PostgreSQL connection
- Firestore initialization
- AWS S3 connectivity
- S3 upload/delete diagnostic
- auth flow
- user flow
- vendor profile creation
- vendor dashboard access after token refresh

## What Is Still Remaining

### 1. Full endpoint-by-endpoint manual testing

Still needs complete testing for:

- product lifecycle flow
- asset upload through actual app-style payloads
- admin approval flow
- marketplace listing flow
- AR endpoint flow
- templates CRUD flow
- reviews flow
- reports flow

### 2. Seed data and demo data setup

Still needed:

- admin test user setup
- vendor demo user setup
- realistic sample products
- product asset demo files
- review/report sample data

### 3. Automated tests

Not implemented yet:

- unit tests
- integration tests
- API flow tests

Suggested priority test areas:

- auth sync and me flow
- vendor creation and approval
- product creation and submission
- marketplace approved-only filtering
- template CRUD

### 4. Better file upload strategy

Current asset upload works through base64 JSON, which is acceptable for initial FYP development, but still needs improvement if desired:

- multipart upload support
- stronger size validation
- better MIME/file validation
- optional signed URL upload flow

### 5. Production hardening

Still missing or incomplete:

- rate limiting
- security headers
- stricter input sanitization
- environment validation improvements
- structured logging strategy
- database migration system
- test coverage

### 6. Analytics module implementation

The required `analytics` module folder exists, but only as a placeholder right now. Business logic and endpoints still need to be finalized if your FYP scope requires analytics features.

### 7. Frontend and Unity integration testing

Still needed:

- frontend integration with Firebase token flow
- vendor dashboard integration
- marketplace integration
- Unity AR endpoint integration
- error-handling verification from client side

## Recommended Next Steps

1. Complete product and asset flow testing.
2. Create or seed an admin user.
3. Test vendor approval and product approval flow.
4. Test marketplace and AR endpoints with approved products.
5. Add automated tests for critical flows.
6. Prepare integration notes for frontend and Unity teams.

## Run the Project

Install dependencies:

```bash
npm install
```

Run in development:

```bash
npm run dev
```

Run in production mode:

```bash
npm start
```

## Required Environment Variables

See:

- [.env.example](D:\Programming Projects\RenovAR\.env.example)

Main required services:

- Firebase Admin credentials
- PostgreSQL `DATABASE_URL`
- AWS S3 credentials and bucket
- Firestore project configuration

## Notes

- Firebase Auth is the source of authentication.
- PostgreSQL stores relational business data.
- Firestore stores template documents.
- S3 stores uploaded asset files.
- Vendor/admin access depends on Firebase token claims, so tokens should be refreshed after role changes.
