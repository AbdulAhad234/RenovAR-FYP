// Standardized error messages
const ERROR_MESSAGES = {
  // Authentication errors
  INVALID_TOKEN: 'Invalid or expired authentication token',
  MISSING_TOKEN: 'Authentication token is required',
  UNAUTHORIZED: 'You are not authorized to perform this action',
  FORBIDDEN: 'Access forbidden',

  // User errors
  USER_NOT_FOUND: 'User not found',
  USER_ALREADY_EXISTS: 'User already exists',
  INVALID_CREDENTIALS: 'Invalid email or password',

  // Product errors
  PRODUCT_NOT_FOUND: 'Product not found',
  PRODUCT_NOT_OWNED: 'You do not own this product',
  PRODUCT_ALREADY_APPROVED: 'Product is already approved',

  // Vendor errors
  VENDOR_NOT_FOUND: 'Vendor profile not found',
  VENDOR_ALREADY_EXISTS: 'Vendor profile already exists',
  VENDOR_NOT_APPROVED: 'Vendor account is not approved yet',

  // Asset errors
  ASSET_NOT_FOUND: 'Asset not found',
  INVALID_FILE_TYPE: 'Invalid file type',
  FILE_TOO_LARGE: 'File size exceeds limit',

  // Validation errors
  VALIDATION_ERROR: 'Validation failed',
  REQUIRED_FIELD: 'This field is required',
  INVALID_EMAIL: 'Invalid email format',
  INVALID_PHONE: 'Invalid phone number format',

  // Database errors
  DATABASE_ERROR: 'Database operation failed',
  CONNECTION_ERROR: 'Database connection failed',

  // External service errors
  FIREBASE_ERROR: 'Firebase service error',
  S3_ERROR: 'AWS S3 service error',

  // General errors
  INTERNAL_ERROR: 'Internal server error',
  NOT_FOUND: 'Resource not found',
  BAD_REQUEST: 'Bad request',
  METHOD_NOT_ALLOWED: 'Method not allowed'
};

module.exports = ERROR_MESSAGES;