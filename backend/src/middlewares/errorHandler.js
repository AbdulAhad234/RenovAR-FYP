// Centralized error handling middleware
const { logError } = require('../utils/errorLogger');
const { errorResponse } = require('../utils/responseFormatter');

/**
 * Global error handler middleware
 * Should be the last middleware in the Express app
 */
const errorHandler = (err, req, res, next) => {
  // Log the error
  logError(err, req, 'Global error handler');

  // Default error properties
  let statusCode = err.statusCode || 500;
  let message = err.message || 'Internal server error';
  let code = err.code || 'INTERNAL_ERROR';

  // Handle specific error types
  if (err.name === 'ValidationError') {
    // Express-validator errors
    statusCode = 400;
    message = 'Validation failed';
    code = 'VALIDATION_ERROR';

    // Format validation errors
    if (err.errors && Array.isArray(err.errors)) {
      const validationErrors = err.errors.map(error => ({
        field: error.path || error.param,
        message: error.msg,
        value: error.value
      }));

      return errorResponse(res, message, code, statusCode, { validationErrors });
    }
  } else if (err.name === 'UnauthorizedError') {
    // JWT or authentication errors
    statusCode = 401;
    message = 'Unauthorized';
    code = 'UNAUTHORIZED';
  } else if (err.name === 'ForbiddenError') {
    // Authorization errors
    statusCode = 403;
    message = 'Forbidden';
    code = 'FORBIDDEN';
  } else if (err.name === 'NotFoundError') {
    // Resource not found errors
    statusCode = 404;
    message = 'Resource not found';
    code = 'NOT_FOUND';
  } else if (err.code === '23505') {
    // PostgreSQL unique constraint violation
    statusCode = 409;
    message = 'Resource already exists';
    code = 'CONFLICT';
  } else if (err.code === '23503') {
    // PostgreSQL foreign key constraint violation
    statusCode = 400;
    message = 'Invalid reference';
    code = 'BAD_REQUEST';
  } else if (err.code === 'ECONNREFUSED' || err.code === 'ENOTFOUND') {
    // Database connection errors
    statusCode = 503;
    message = 'Service temporarily unavailable';
    code = 'SERVICE_UNAVAILABLE';
  }

  // Handle Firebase errors
  if (err.code && err.code.startsWith('auth/')) {
    statusCode = 401;
    message = 'Authentication failed';
    code = 'AUTH_ERROR';
  }

  // Handle AWS S3 errors
  if (err.name === 'NoSuchKey') {
    statusCode = 404;
    message = 'File not found';
    code = 'NOT_FOUND';
  } else if (err.name === 'AccessDenied') {
    statusCode = 403;
    message = 'Access denied to resource';
    code = 'FORBIDDEN';
  }

  // Send error response
  return errorResponse(res, message, code, statusCode, err.details || null);
};

/**
 * Async error wrapper for route handlers
 * Wraps async route handlers to catch rejected promises
 */
const asyncHandler = (fn) => {
  return (req, res, next) => {
    Promise.resolve(fn(req, res, next)).catch(next);
  };
};

/**
 * Create custom error classes
 */
class ValidationError extends Error {
  constructor(message, code = 'VALIDATION_ERROR') {
    super(message);
    this.name = 'ValidationError';
    this.code = code;
    this.statusCode = 400;
  }
}

class UnauthorizedError extends Error {
  constructor(message = 'Unauthorized') {
    super(message);
    this.name = 'UnauthorizedError';
    this.code = 'UNAUTHORIZED';
    this.statusCode = 401;
  }
}

class ForbiddenError extends Error {
  constructor(message = 'Forbidden') {
    super(message);
    this.name = 'ForbiddenError';
    this.code = 'FORBIDDEN';
    this.statusCode = 403;
  }
}

class NotFoundError extends Error {
  constructor(message = 'Resource not found') {
    super(message);
    this.name = 'NotFoundError';
    this.code = 'NOT_FOUND';
    this.statusCode = 404;
  }
}

class ConflictError extends Error {
  constructor(message = 'Resource already exists') {
    super(message);
    this.name = 'ConflictError';
    this.code = 'CONFLICT';
    this.statusCode = 409;
  }
}

module.exports = {
  errorHandler,
  asyncHandler,
  ValidationError,
  UnauthorizedError,
  ForbiddenError,
  NotFoundError,
  ConflictError
};
