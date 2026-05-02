// AWS S3 bucket names and configurations
const S3_BUCKETS = {
  MAIN: process.env.AWS_S3_BUCKET_NAME,
  IMAGES: process.env.AWS_S3_BUCKET_NAME, // Same bucket for now, can be separated later
  MODELS: process.env.AWS_S3_BUCKET_NAME,
};

// Asset types for S3 organization
const ASSET_TYPES = {
  IMAGE: "image",
  MODEL: "model",
  THUMBNAIL: "thumbnail",
};

ASSET_TYPES.TEXTURE = "texture";

module.exports = {
  S3_BUCKETS,
  ASSET_TYPES,
};
