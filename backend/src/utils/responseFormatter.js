// Standardized response formatting utilities

/**
 * Success response formatter
 * @param {Object} res - Express response object
 * @param {any} data - Response data
 * @param {string} message - Success message
 * @param {number} statusCode - HTTP status code (default: 200)
 */
const successResponse = (res, data = null, message = 'Success', statusCode = 200) => {
  const response = {
    success: true,
    message,
    ...(data !== null && { data })
  };

  return res.status(statusCode).json(response);
};

/**
 * Error response formatter
 * @param {Object} res - Express response object
 * @param {string} message - Error message
 * @param {string} code - Error code for client handling
 * @param {number} statusCode - HTTP status code (default: 500)
 * @param {Object} details - Additional error details (optional)
 */
const errorResponse = (res, message, code = 'INTERNAL_ERROR', statusCode = 500, details = null) => {
  const response = {
    success: false,
    message,
    code,
    ...(details && { details })
  };

  return res.status(statusCode).json(response);
};

/**
 * Pagination metadata formatter
 * @param {number} page - Current page
 * @param {number} limit - Items per page
 * @param {number} total - Total items
 * @param {Array} data - Paginated data
 * @returns {Object} Formatted pagination response
 */
const paginationResponse = (page, limit, total, data) => {
  const totalPages = Math.ceil(total / limit);
  const hasNext = page < totalPages;
  const hasPrev = page > 1;

  return {
    data,
    pagination: {
      page,
      limit,
      total,
      totalPages,
      hasNext,
      hasPrev
    }
  };
};

module.exports = {
  successResponse,
  errorResponse,
  paginationResponse
};