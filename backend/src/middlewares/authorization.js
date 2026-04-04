// Authorization middleware for role-based access control
const { errorResponse } = require('../utils/responseFormatter');
const ERROR_MESSAGES = require('../constants/errorMessages');
const ROLES = require('../constants/roles');

/**
 * Middleware to check if user has required role
 * @param {string|string[]} requiredRoles - Required role(s)
 * @returns {Function} Middleware function
 */
const requireRole = (requiredRoles) => {
  return (req, res, next) => {
    // Check if user is authenticated
    if (!req.user) {
      return errorResponse(
        res,
        ERROR_MESSAGES.UNAUTHORIZED,
        'UNAUTHORIZED',
        401
      );
    }

    // Convert single role to array for consistent handling
    const roles = Array.isArray(requiredRoles) ? requiredRoles : [requiredRoles];

    // Check if user has one of the required roles
    if (!roles.includes(req.user.role)) {
      return errorResponse(
        res,
        ERROR_MESSAGES.FORBIDDEN,
        'FORBIDDEN',
        403
      );
    }

    next();
  };
};

/**
 * Specific role requirement middlewares
 */
const requireCustomer = requireRole(ROLES.CUSTOMER);
const requireVendor = requireRole(ROLES.VENDOR);
const requireAdmin = requireRole(ROLES.ADMIN);

// Allow customer or vendor (for shared endpoints)
const requireCustomerOrVendor = requireRole([ROLES.CUSTOMER, ROLES.VENDOR]);

// Allow vendor or admin (for vendor management)
const requireVendorOrAdmin = requireRole([ROLES.VENDOR, ROLES.ADMIN]);

/**
 * Ownership check middleware factory
 * @param {Function} ownershipChecker - Function that checks ownership
 * @returns {Function} Middleware function
 */
const requireOwnership = (ownershipChecker) => {
  return async (req, res, next) => {
    try {
      if (!req.user) {
        return errorResponse(
          res,
          ERROR_MESSAGES.UNAUTHORIZED,
          'UNAUTHORIZED',
          401
        );
      }

      const hasOwnership = await ownershipChecker(req.user.uid, req.params);

      if (!hasOwnership) {
        return errorResponse(
          res,
          ERROR_MESSAGES.FORBIDDEN,
          'FORBIDDEN',
          403
        );
      }

      next();
    } catch (error) {
      console.error('Ownership check error:', error.message);
      return errorResponse(
        res,
        ERROR_MESSAGES.INTERNAL_ERROR,
        'INTERNAL_ERROR',
        500
      );
    }
  };
};

module.exports = {
  requireRole,
  requireCustomer,
  requireVendor,
  requireAdmin,
  requireCustomerOrVendor,
  requireVendorOrAdmin,
  requireOwnership
};