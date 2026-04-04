// Request validation middleware wrapper
const { validationResult } = require('express-validator');
const { errorResponse } = require('../utils/responseFormatter');
const ERROR_MESSAGES = require('../constants/errorMessages');

/**
 * Middleware to handle validation results from express-validator
 * Should be called after validation rules in route handlers
 */
const handleValidationErrors = (req, res, next) => {
  const errors = validationResult(req);

  if (!errors.isEmpty()) {
    const validationErrors = errors.array().map(error => ({
      field: error.path || error.param,
      message: error.msg,
      value: error.value,
      location: error.location
    }));

    return errorResponse(
      res,
      ERROR_MESSAGES.VALIDATION_ERROR,
      'VALIDATION_ERROR',
      400,
      { validationErrors }
    );
  }

  next();
};

/**
 * Wrapper to combine validation rules with error handling
 * @param {Array} validations - Array of express-validator validation chains
 * @returns {Array} Array of middleware functions
 */
const validateRequest = (validations) => {
  return [...validations, handleValidationErrors];
};

module.exports = {
  handleValidationErrors,
  validateRequest
};
