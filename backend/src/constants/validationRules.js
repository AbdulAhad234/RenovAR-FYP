// Validation rules and constraints
const VALIDATION_RULES = {
  // String lengths
  NAME_MIN_LENGTH: 2,
  NAME_MAX_LENGTH: 100,
  DESCRIPTION_MAX_LENGTH: 1000,
  COMMENT_MAX_LENGTH: 500,

  // Email regex
  EMAIL_REGEX: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,

  // Phone regex (international format)
  PHONE_REGEX: /^\+?[1-9]\d{1,14}$/,

  // Password requirements
  PASSWORD_MIN_LENGTH: 8,

  // File size limits (in bytes)
  IMAGE_MAX_SIZE: 5 * 1024 * 1024, // 5MB
  MODEL_MAX_SIZE: 50 * 1024 * 1024, // 50MB

  // Rating constraints
  RATING_MIN: 1,
  RATING_MAX: 5,

  // Pagination defaults
  DEFAULT_PAGE: 1,
  DEFAULT_LIMIT: 20,
  MAX_LIMIT: 100,

  // Price constraints
  PRICE_MIN: 0,
  PRICE_MAX: 999999.99,

  // Dimensions constraints
  DIMENSION_MIN: 0.1,
  DIMENSION_MAX: 1000
};

module.exports = VALIDATION_RULES;