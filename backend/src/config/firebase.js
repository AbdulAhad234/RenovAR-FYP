// Firebase Admin SDK configuration
const { initializeFirebase, getAuth } = require('../utils/tokenHelper');

// Initialize Firebase on module load
initializeFirebase();

// Export Firebase services
const auth = getAuth();

module.exports = {
  auth
};