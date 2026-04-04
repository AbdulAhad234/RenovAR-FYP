// Error logging and handling utilities
const morgan = require('morgan');

/**
 * Custom error logger for application errors
 * @param {Error} error - Error object
 * @param {Object} req - Express request object (optional)
 * @param {string} context - Additional context information
 */
const logError = (error, req = null, context = '') => {
  const timestamp = new Date().toISOString();
  const logLevel = process.env.LOG_LEVEL || 'info';

  let logMessage = `[${timestamp}] ERROR: ${error.message}`;

  if (context) {
    logMessage += ` | Context: ${context}`;
  }

  if (req) {
    logMessage += ` | Method: ${req.method} | URL: ${req.originalUrl} | IP: ${req.ip}`;
  }

  console.error(logMessage);

  // Log stack trace in development
  if (process.env.NODE_ENV === 'development' && error.stack) {
    console.error(error.stack);
  }

  // TODO: In production, send to logging service (e.g., Winston, CloudWatch, etc.)
};

/**
 * Morgan stream for HTTP request logging
 */
const morganStream = {
  write: (message) => {
    // Remove trailing newline from Morgan
    const cleanMessage = message.trim();
    console.log(`[${new Date().toISOString()}] ${cleanMessage}`);
  }
};

/**
 * Morgan configuration for HTTP logging
 */
const morganConfig = morgan(
  ':remote-addr - :remote-user [:date[clf]] ":method :url HTTP/:http-version" :status :res[content-length] ":referrer" ":user-agent"',
  {
    stream: morganStream,
    skip: (req, res) => process.env.NODE_ENV === 'test'
  }
);

module.exports = {
  logError,
  morganStream,
  morganConfig
};