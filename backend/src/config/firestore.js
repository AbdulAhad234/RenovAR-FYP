// Firestore configuration
const { Firestore } = require('@google-cloud/firestore');
const path = require('path');

// Initialize Firestore client
let firestore;

try {
  // If GOOGLE_APPLICATION_CREDENTIALS is set, use it
  if (process.env.GOOGLE_APPLICATION_CREDENTIALS) {
    firestore = new Firestore({
      keyFilename: path.resolve(process.env.GOOGLE_APPLICATION_CREDENTIALS),
      projectId: process.env.FIREBASE_PROJECT_ID
    });
  } else {
    // Use default credentials (for production environments)
    firestore = new Firestore({
      projectId: process.env.FIREBASE_PROJECT_ID
    });
  }

  console.log('✅ Firestore client initialized successfully');
} catch (error) {
  console.error('❌ Failed to initialize Firestore client:', error.message);
  // Don't throw error here - allow app to start, but Firestore operations will fail
  firestore = null;
}

// Collection references
const collections = {
  TEMPLATES: 'templates',
  AR_CONFIGS: 'ar_configs'
};

// Helper function to get collection reference
const getCollection = (collectionName) => {
  if (!firestore) {
    throw new Error('Firestore client not initialized');
  }
  return firestore.collection(collectionName);
};

// Test Firestore connection
const testConnection = async () => {
  try {
    if (!firestore) return false;

    // Try to get a document that doesn't exist to test connection
    const testDoc = await firestore.collection('test').doc('connection-test').get();
    console.log('✅ Firestore connection test successful');
    return true;
  } catch (error) {
    console.error('❌ Firestore connection test failed:', error.message);
    return false;
  }
};

module.exports = {
  firestore,
  collections,
  getCollection,
  testConnection
};