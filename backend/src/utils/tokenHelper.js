// Firebase token verification helper
const admin = require('firebase-admin');
const { logError } = require('./errorLogger');

/**
 * Initialize Firebase Admin SDK if not already initialized
 */
const initializeFirebase = () => {
  if (!admin.apps.length) {
    const serviceAccount = {
      type: 'service_account',
      project_id: process.env.FIREBASE_PROJECT_ID,
      private_key_id: process.env.FIREBASE_PRIVATE_KEY_ID,
      private_key: process.env.FIREBASE_PRIVATE_KEY.replace(/\\n/g, '\n'),
      client_email: process.env.FIREBASE_CLIENT_EMAIL,
      client_id: process.env.FIREBASE_CLIENT_ID,
      auth_uri: process.env.FIREBASE_AUTH_URI,
      token_uri: process.env.FIREBASE_TOKEN_URI,
      auth_provider_x509_cert_url: process.env.FIREBASE_AUTH_PROVIDER_X509_CERT_URL,
      client_x509_cert_url: process.env.FIREBASE_CLIENT_X509_CERT_URL
    };

    admin.initializeApp({
      credential: admin.credential.cert(serviceAccount),
      projectId: process.env.FIREBASE_PROJECT_ID
    });
  }
};

/**
 * Verify Firebase ID token
 * @param {string} token - Firebase ID token
 * @returns {Promise<Object>} Decoded token payload
 */
const verifyFirebaseToken = async (token) => {
  try {
    initializeFirebase();
    const decodedToken = await admin.auth().verifyIdToken(token);
    return decodedToken;
  } catch (error) {
    logError(error, null, 'Firebase token verification failed');
    throw new Error('Invalid Firebase token');
  }
};

/**
 * Get Firebase Auth instance
 * @returns {Object} Firebase Auth instance
 */
const getAuth = () => {
  initializeFirebase();
  return admin.auth();
};

/**
 * Set custom user claims (for role management)
 * @param {string} uid - Firebase user ID
 * @param {Object} claims - Custom claims to set
 * @returns {Promise<void>}
 */
const setCustomClaims = async (uid, claims) => {
  try {
    initializeFirebase();
    await admin.auth().setCustomUserClaims(uid, claims);
  } catch (error) {
    logError(error, null, `Failed to set custom claims for user ${uid}`);
    throw new Error('Failed to update user claims');
  }
};

/**
 * Get user by Firebase UID
 * @param {string} uid - Firebase user ID
 * @returns {Promise<Object>} User record
 */
const getUserByUid = async (uid) => {
  try {
    initializeFirebase();
    const userRecord = await admin.auth().getUser(uid);
    return userRecord;
  } catch (error) {
    logError(error, null, `Failed to get user by UID ${uid}`);
    throw new Error('User not found');
  }
};

/**
 * Create Firebase user
 * @param {Object} userData - User data
 * @param {string} userData.email - User email
 * @param {string} userData.displayName - User display name
 * @returns {Promise<Object>} Created user record
 */
const createFirebaseUser = async (userData) => {
  try {
    initializeFirebase();
    const userRecord = await admin.auth().createUser({
      email: userData.email,
      displayName: userData.displayName,
      emailVerified: false
    });
    return userRecord;
  } catch (error) {
    logError(error, null, `Failed to create Firebase user: ${userData.email}`);
    throw new Error('Failed to create user account');
  }
};

/**
 * Update Firebase user
 * @param {string} uid - Firebase user ID
 * @param {Object} updates - Fields to update
 * @returns {Promise<Object>} Updated user record
 */
const updateFirebaseUser = async (uid, updates) => {
  try {
    initializeFirebase();
    const userRecord = await admin.auth().updateUser(uid, updates);
    return userRecord;
  } catch (error) {
    logError(error, null, `Failed to update Firebase user ${uid}`);
    throw new Error('Failed to update user account');
  }
};

module.exports = {
  initializeFirebase,
  verifyFirebaseToken,
  getAuth,
  setCustomClaims,
  getUserByUid,
  createFirebaseUser,
  updateFirebaseUser
};