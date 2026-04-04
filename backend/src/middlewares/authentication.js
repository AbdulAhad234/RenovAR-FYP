// Authentication middleware for Firebase token verification
const { verifyFirebaseToken } = require('../utils/tokenHelper');
const { errorResponse } = require('../utils/responseFormatter');
const ERROR_MESSAGES = require('../constants/errorMessages');

/**
 * Middleware to verify Firebase ID token and attach user to request
 * Expects Authorization header: "Bearer <token>"
 */
const authenticate = async (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return errorResponse(
        res,
        ERROR_MESSAGES.MISSING_TOKEN,
        'MISSING_TOKEN',
        401
      );
    }

    const token = authHeader.substring(7); // Remove "Bearer " prefix

    if (!token) {
      return errorResponse(
        res,
        ERROR_MESSAGES.MISSING_TOKEN,
        'MISSING_TOKEN',
        401
      );
    }

    // Verify Firebase token
    const decodedToken = await verifyFirebaseToken(token);

    // Attach user information to request object
    req.user = {
      uid: decodedToken.uid,
      email: decodedToken.email,
      emailVerified: decodedToken.email_verified,
      displayName: decodedToken.name || null,
      photoURL: decodedToken.picture || null,
      // Custom claims (roles) will be added here when set
      role: decodedToken.role || 'customer' // Default to customer if no role set
    };

    next();
  } catch (error) {
    console.error('Authentication error:', error.message);

    return errorResponse(
      res,
      ERROR_MESSAGES.INVALID_TOKEN,
      'INVALID_TOKEN',
      401
    );
  }
};

/**
 * Optional authentication middleware (doesn't fail if no token)
 * Useful for endpoints that work with or without authentication
 */
const optionalAuthenticate = async (req, res, next) => {
  try {
    const authHeader = req.headers.authorization;

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      // No token provided, continue without user
      req.user = null;
      return next();
    }

    const token = authHeader.substring(7);

    if (!token) {
      req.user = null;
      return next();
    }

    const decodedToken = await verifyFirebaseToken(token);

    req.user = {
      uid: decodedToken.uid,
      email: decodedToken.email,
      emailVerified: decodedToken.email_verified,
      displayName: decodedToken.name || null,
      photoURL: decodedToken.picture || null,
      role: decodedToken.role || 'customer'
    };

    next();
  } catch (error) {
    // Token invalid, but don't fail - continue without user
    console.warn('Optional authentication failed:', error.message);
    req.user = null;
    next();
  }
};

module.exports = {
  authenticate,
  optionalAuthenticate
};