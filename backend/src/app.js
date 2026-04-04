const express = require('express');
const corsMiddleware = require('./middlewares/cors');
const { morganConfig } = require('./utils/errorLogger');
const { errorHandler } = require('./middlewares/errorHandler');
const apiExamples = require('./docs/apiExamples');
const authRoutes = require('./modules/auth/routes');
const userRoutes = require('./modules/users/routes');
const vendorRoutes = require('./modules/vendors/routes');
const productRoutes = require('./modules/products/routes');
const assetRoutes = require('./modules/assets/routes');
const marketplaceRoutes = require('./modules/marketplace/routes');
const arRoutes = require('./modules/ar/routes');
const templateRoutes = require('./modules/templates/routes');
const reviewRoutes = require('./modules/reviews/routes');
const reportRoutes = require('./modules/reports/routes');
const adminRoutes = require('./modules/admin/routes');
const analyticsRoutes = require('./modules/analytics/routes');

// Create Express application
const app = express();

// CORS middleware (must be first)
app.use(corsMiddleware);

// HTTP request logging
app.use(morganConfig);

// Body parsing middleware
app.use(express.json({ limit: '10mb' })); // Parse JSON bodies
app.use(express.urlencoded({ extended: true, limit: '10mb' })); // Parse URL-encoded bodies

// Health check endpoint
app.get('/health', (req, res) => {
  res.status(200).json({
    success: true,
    message: 'RenovAR Backend is healthy',
    timestamp: new Date().toISOString(),
    environment: process.env.NODE_ENV || 'development'
  });
});

// Root endpoint
app.get('/', (req, res) => {
  res.status(200).json({
    success: true,
    message: 'Welcome to RenovAR Backend API',
    version: '1.0.0',
    docs: '/docs'
  });
});

app.get('/docs', (req, res) => {
  res.status(200).json({
    success: true,
    message: 'RenovAR API examples',
    data: apiExamples
  });
});

app.use('/api/auth', authRoutes);
app.use('/api/users', userRoutes);
app.use('/api/vendors', vendorRoutes);
app.use('/api/products', productRoutes);
app.use('/api/assets', assetRoutes);
app.use('/api/marketplace', marketplaceRoutes);
app.use('/api/ar', arRoutes);
app.use('/api/templates', templateRoutes);
app.use('/api/reviews', reviewRoutes);
app.use('/api/reports', reportRoutes);
app.use('/api/admin', adminRoutes);
app.use('/api/analytics', analyticsRoutes);

// 404 handler for undefined routes
app.use('*', (req, res) => {
  res.status(404).json({
    success: false,
    message: `Route ${req.originalUrl} not found`,
    code: 'ROUTE_NOT_FOUND'
  });
});

// Global error handler (must be last)
app.use(errorHandler);

module.exports = app;
