// AWS S3 configuration
const { isS3Configured } = require('../utils/s3Helper');

// Test S3 configuration on module load
const testS3Connection = async () => {
  try {
    const isConfigured = await isS3Configured();
    if (isConfigured) {
      console.log('✅ AWS S3 client configured successfully');
    } else {
      console.warn('⚠️ AWS S3 client configuration incomplete - S3 operations may fail');
    }
    return isConfigured;
  } catch (error) {
    console.error('❌ AWS S3 configuration test failed:', error.message);
    return false;
  }
};

// Run configuration test
testS3Connection();

module.exports = {
  testS3Connection
};