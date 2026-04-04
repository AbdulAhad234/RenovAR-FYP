// Reusable validation helper functions
const VALIDATION_RULES = require('../constants/validationRules');

/**
 * Email validation
 * @param {string} email - Email to validate
 * @returns {boolean} True if valid
 */
const isValidEmail = (email) => {
  return VALIDATION_RULES.EMAIL_REGEX.test(email);
};

/**
 * Phone number validation
 * @param {string} phone - Phone number to validate
 * @returns {boolean} True if valid
 */
const isValidPhone = (phone) => {
  return VALIDATION_RULES.PHONE_REGEX.test(phone);
};

/**
 * Password strength validation
 * @param {string} password - Password to validate
 * @returns {boolean} True if valid
 */
const isValidPassword = (password) => {
  return password && password.length >= VALIDATION_RULES.PASSWORD_MIN_LENGTH;
};

/**
 * Name validation (length and basic character check)
 * @param {string} name - Name to validate
 * @returns {boolean} True if valid
 */
const isValidName = (name) => {
  if (!name || typeof name !== 'string') return false;
  const length = name.trim().length;
  return length >= VALIDATION_RULES.NAME_MIN_LENGTH && length <= VALIDATION_RULES.NAME_MAX_LENGTH;
};

/**
 * Rating validation
 * @param {number} rating - Rating to validate
 * @returns {boolean} True if valid
 */
const isValidRating = (rating) => {
  return Number.isInteger(rating) &&
         rating >= VALIDATION_RULES.RATING_MIN &&
         rating <= VALIDATION_RULES.RATING_MAX;
};

/**
 * Price validation
 * @param {number} price - Price to validate
 * @returns {boolean} True if valid
 */
const isValidPrice = (price) => {
  return typeof price === 'number' &&
         price >= VALIDATION_RULES.PRICE_MIN &&
         price <= VALIDATION_RULES.PRICE_MAX;
};

/**
 * File size validation
 * @param {number} size - File size in bytes
 * @param {string} type - File type ('image' or 'model')
 * @returns {boolean} True if valid
 */
const isValidFileSize = (size, type) => {
  if (type === 'image') {
    return size <= VALIDATION_RULES.IMAGE_MAX_SIZE;
  } else if (type === 'model') {
    return size <= VALIDATION_RULES.MODEL_MAX_SIZE;
  }
  return false;
};

/**
 * MIME type validation for images
 * @param {string} mimeType - MIME type to validate
 * @returns {boolean} True if valid image type
 */
const isValidImageType = (mimeType) => {
  const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
  return validTypes.includes(mimeType);
};

/**
 * MIME type validation for 3D models
 * @param {string} mimeType - MIME type to validate
 * @returns {boolean} True if valid model type
 */
const isValidModelType = (mimeType) => {
  const validTypes = [
    'model/gltf+json',
    'model/gltf-binary',
    'application/octet-stream', // For .glb files
    'model/usd',
    'model/usdz'
  ];
  return validTypes.includes(mimeType);
};

/**
 * UUID validation
 * @param {string} uuid - UUID string to validate
 * @returns {boolean} True if valid UUID
 */
const isValidUUID = (uuid) => {
  const uuidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  return uuidRegex.test(uuid);
};

/**
 * Sanitize string input (trim and basic XSS prevention)
 * @param {string} input - Input string to sanitize
 * @returns {string} Sanitized string
 */
const sanitizeString = (input) => {
  if (typeof input !== 'string') return '';
  return input.trim().replace(/[<>]/g, '');
};

module.exports = {
  isValidEmail,
  isValidPhone,
  isValidPassword,
  isValidName,
  isValidRating,
  isValidPrice,
  isValidFileSize,
  isValidImageType,
  isValidModelType,
  isValidUUID,
  sanitizeString
};