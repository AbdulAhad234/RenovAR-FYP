// AWS S3 helper functions
const { S3Client, PutObjectCommand, GetObjectCommand, HeadObjectCommand, DeleteObjectCommand, ListObjectsV2Command } = require('@aws-sdk/client-s3');
const { v4: uuidv4 } = require('uuid');
const { S3_BUCKETS, ASSET_TYPES } = require('../constants/s3Buckets');
const { logError } = require('./errorLogger');

// Initialize S3 client
const s3Client = new S3Client({
  region: process.env.AWS_REGION,
  credentials: {
    accessKeyId: process.env.AWS_ACCESS_KEY_ID,
    secretAccessKey: process.env.AWS_SECRET_ACCESS_KEY
  }
});

/**
 * Generate unique S3 key for asset
 * @param {string} productId - Product ID
 * @param {string} assetType - Asset type (image, model, thumbnail)
 * @param {string} originalFileName - Original file name
 * @returns {string} S3 key
 */
const generateS3Key = (productId, assetType, originalFileName) => {
  const extension = originalFileName.split('.').pop();
  const uuid = uuidv4();
  return `products/${productId}/${assetType}s/${uuid}.${extension}`;
};

const buildS3Url = (key, bucket = S3_BUCKETS.MAIN) => {
  return `https://${bucket}.s3.${process.env.AWS_REGION}.amazonaws.com/${key}`;
};

/**
 * Upload file to S3
 * @param {Buffer} fileBuffer - File buffer
 * @param {string} key - S3 key
 * @param {string} contentType - MIME type
 * @param {string} bucket - S3 bucket name
 * @returns {Promise<Object>} Upload result
 */
const uploadToS3 = async (fileBuffer, key, contentType, bucket = S3_BUCKETS.MAIN) => {
  try {
    const command = new PutObjectCommand({
      Bucket: bucket,
      Key: key,
      Body: fileBuffer,
      ContentType: contentType,
      // Add ACL if needed: ACL: 'public-read'
    });

    const result = await s3Client.send(command);
    return {
      success: true,
      key,
      bucket,
      location: buildS3Url(key, bucket)
    };
  } catch (error) {
    logError(error, null, `S3 upload failed for key: ${key}`);
    throw new Error('Failed to upload file to S3');
  }
};

/**
 * Delete file from S3
 * @param {string} key - S3 key
 * @param {string} bucket - S3 bucket name
 * @returns {Promise<Object>} Delete result
 */
const deleteFromS3 = async (key, bucket = S3_BUCKETS.MAIN) => {
  try {
    const command = new DeleteObjectCommand({
      Bucket: bucket,
      Key: key
    });

    await s3Client.send(command);
    return { success: true, key, bucket };
  } catch (error) {
    logError(error, null, `S3 delete failed for key: ${key}`);
    throw new Error('Failed to delete file from S3');
  }
};

/**
 * Generate signed URL for private S3 object (if using private ACL)
 * @param {string} key - S3 key
 * @param {number} expiresIn - URL expiration time in seconds (default: 3600 = 1 hour)
 * @param {string} bucket - S3 bucket name
 * @returns {Promise<string>} Signed URL
 */
const generateSignedUrl = async (key, expiresIn = 3600, bucket = S3_BUCKETS.MAIN) => {
  try {
    const { getSignedUrl } = require('@aws-sdk/s3-request-presigner');
    const command = new GetObjectCommand({
      Bucket: bucket,
      Key: key
    });

    const signedUrl = await getSignedUrl(s3Client, command, { expiresIn });
    return signedUrl;
  } catch (error) {
    logError(error, null, `Failed to generate signed URL for key: ${key}`);
    throw new Error('Failed to generate signed URL');
  }
};

const generatePresignedUploadUrl = async (key, contentType, expiresIn = 900, bucket = S3_BUCKETS.MAIN) => {
  try {
    const { getSignedUrl } = require('@aws-sdk/s3-request-presigner');
    const command = new PutObjectCommand({
      Bucket: bucket,
      Key: key,
      ContentType: contentType
    });

    return getSignedUrl(s3Client, command, { expiresIn });
  } catch (error) {
    logError(error, null, `Failed to generate presigned upload URL for key: ${key}`);
    throw new Error('Failed to generate upload URL');
  }
};

const objectExists = async (key, bucket = S3_BUCKETS.MAIN) => {
  try {
    await s3Client.send(new HeadObjectCommand({
      Bucket: bucket,
      Key: key
    }));
    return true;
  } catch (error) {
    if (error.name === 'NotFound' || error.$metadata?.httpStatusCode === 404) {
      return false;
    }

    logError(error, null, `Failed to validate S3 object existence for key: ${key}`);
    throw new Error('Failed to validate uploaded file');
  }
};

/**
 * Get public URL for S3 object (if using public-read ACL)
 * @param {string} key - S3 key
 * @param {string} bucket - S3 bucket name
 * @returns {string} Public URL
 */
const getPublicUrl = (key, bucket = S3_BUCKETS.MAIN) => {
  return buildS3Url(key, bucket);
};

/**
 * Check if S3 client is properly configured
 * @returns {Promise<boolean>} True if configured
 */
const isS3Configured = async () => {
  try {
    // Try to list objects (will fail with access denied if credentials are wrong)
    await s3Client.send(new ListObjectsV2Command({
      Bucket: S3_BUCKETS.MAIN,
      MaxKeys: 1
    }));
    return true;
  } catch (error) {
    // If it's an access denied error, credentials are configured but permissions might be wrong
    if (error.name === 'AccessDenied') {
      return true;
    }
    return false;
  }
};

module.exports = {
  s3Client,
  generateS3Key,
  uploadToS3,
  deleteFromS3,
  generateSignedUrl,
  generatePresignedUploadUrl,
  objectExists,
  getPublicUrl,
  buildS3Url,
  isS3Configured
};
