const crypto = require("crypto");
const path = require("path");
const assetRepository = require("./repository");
const productService = require("../products/service");
const {
  generateS3Key,
  uploadToS3,
  generatePresignedUploadUrl,
  generateSignedUrl,
  objectExists,
  buildS3Url,
} = require("../../utils/s3Helper");
const { ApiError } = require("../../utils/apiError");
const {
  isValidFileSize,
  isValidImageType,
  isValidModelType,
} = require("../../utils/validators");

const UPLOAD_URL_EXPIRY_SECONDS = 900;
const DOWNLOAD_URL_EXPIRY_SECONDS = 3600;
const ALLOWED_IMAGE_EXTENSIONS = ["jpg", "jpeg", "png", "webp"];
const ALLOWED_MODEL_EXTENSIONS = ["glb", "fbx"];
const ALLOWED_THUMBNAIL_EXTENSIONS = ["jpg", "jpeg", "png", "webp"];
const ALLOWED_FBX_MIME_TYPES = [
  "application/octet-stream",
  "application/vnd.autodesk.fbx",
  "model/fbx",
];

const getUploadSigningSecret = () => {
  return (
    process.env.ASSET_UPLOAD_SIGNING_SECRET ||
    process.env.FIREBASE_PRIVATE_KEY ||
    process.env.DATABASE_URL ||
    "renovar-upload-signing-secret"
  );
};

const getFileExtension = (fileName) =>
  path
    .extname(fileName || "")
    .replace(".", "")
    .toLowerCase();

const getAllowedExtensions = (assetType) => {
  if (assetType === "model") return ALLOWED_MODEL_EXTENSIONS;
  if (assetType === "thumbnail") return ALLOWED_THUMBNAIL_EXTENSIONS;
  if (assetType === "texture") return ALLOWED_IMAGE_EXTENSIONS;
  return ALLOWED_IMAGE_EXTENSIONS;
};

const assertAssetPayloadIsValid = (payload, mode = "session") => {
  const extension = getFileExtension(payload.fileName);
  const allowedExtensions = getAllowedExtensions(payload.assetType);

  if (!allowedExtensions.includes(extension)) {
    throw new ApiError(
      400,
      `Unsupported ${payload.assetType} file extension`,
      "BAD_REQUEST",
    );
  }

  if (
    (payload.assetType === "image" ||
      payload.assetType === "thumbnail" ||
      payload.assetType === "texture") &&
    !isValidImageType(payload.mimeType)
  ) {
    throw new ApiError(400, "Unsupported image MIME type", "BAD_REQUEST");
  }

  if (payload.assetType === "model") {
    const isGlb = extension === "glb" && isValidModelType(payload.mimeType);
    const isFbx =
      extension === "fbx" && ALLOWED_FBX_MIME_TYPES.includes(payload.mimeType);

    if (!isGlb && !isFbx) {
      throw new ApiError(
        400,
        "Only GLB and FBX model uploads are supported",
        "BAD_REQUEST",
      );
    }
  }

  if (
    payload.fileSize !== undefined &&
    !isValidFileSize(
      Number(payload.fileSize),
      payload.assetType === "thumbnail" ? "image" : payload.assetType,
    )
  ) {
    throw new ApiError(
      400,
      "File size exceeds the allowed limit",
      "BAD_REQUEST",
    );
  }

  if (
    mode === "confirm" &&
    (!payload.s3Key || !payload.uploadToken || !payload.expiresAt)
  ) {
    throw new ApiError(
      400,
      "s3Key, uploadToken, and expiresAt are required to confirm an uploaded asset",
      "BAD_REQUEST",
    );
  }
};

const buildMetadata = (payload) => ({
  ...payload.metadata,
  format: getFileExtension(payload.fileName),
  originalFileName: payload.fileName,
});

const buildUploadToken = ({
  firebaseUid,
  productId,
  assetType,
  s3Key,
  fileName,
  mimeType,
  expiresAt,
}) => {
  const signer = crypto.createHmac("sha256", getUploadSigningSecret());
  signer.update(
    [
      firebaseUid,
      productId,
      assetType,
      s3Key,
      fileName,
      mimeType,
      expiresAt,
    ].join("|"),
  );
  return signer.digest("hex");
};

const createUploadSession = async (productId, firebaseUid, payload) => {
  await productService.assertProductOwnership(productId, firebaseUid);
  assertAssetPayloadIsValid(payload, "session");

  const s3Key = generateS3Key(
    String(productId),
    payload.assetType,
    payload.fileName,
  );
  const expiresAt = new Date(
    Date.now() + UPLOAD_URL_EXPIRY_SECONDS * 1000,
  ).toISOString();
  const uploadToken = buildUploadToken({
    firebaseUid,
    productId,
    assetType: payload.assetType,
    s3Key,
    fileName: payload.fileName,
    mimeType: payload.mimeType,
    expiresAt,
  });

  const uploadUrl = await generatePresignedUploadUrl(
    s3Key,
    payload.mimeType,
    UPLOAD_URL_EXPIRY_SECONDS,
  );

  return {
    assetType: payload.assetType,
    fileName: payload.fileName,
    mimeType: payload.mimeType,
    fileSize: payload.fileSize || null,
    s3Key,
    uploadUrl,
    uploadMethod: "PUT",
    expiresAt,
    uploadToken,
    requiredHeaders: {
      "Content-Type": payload.mimeType,
    },
  };
};

const confirmAssetUpload = async (productId, firebaseUid, payload) => {
  await productService.assertProductOwnership(productId, firebaseUid);
  assertAssetPayloadIsValid(payload, "confirm");

  if (new Date(payload.expiresAt).getTime() < Date.now()) {
    throw new ApiError(400, "Upload session has expired", "BAD_REQUEST");
  }

  const expectedToken = buildUploadToken({
    firebaseUid,
    productId,
    assetType: payload.assetType,
    s3Key: payload.s3Key,
    fileName: payload.fileName,
    mimeType: payload.mimeType,
    expiresAt: payload.expiresAt,
  });

  if (expectedToken !== payload.uploadToken) {
    throw new ApiError(
      400,
      "The uploaded asset was not issued for this product session",
      "BAD_REQUEST",
    );
  }

  const expectedPrefix = `products/${productId}/${payload.assetType}s/`;
  if (!payload.s3Key.startsWith(expectedPrefix)) {
    throw new ApiError(
      400,
      "The uploaded asset key is invalid for this product",
      "BAD_REQUEST",
    );
  }

  const exists = await objectExists(payload.s3Key);
  if (!exists) {
    throw new ApiError(400, "Uploaded file not found in S3", "BAD_REQUEST");
  }

  const existingAsset = await assetRepository.findByProductIdAndS3Key(
    productId,
    payload.s3Key,
  );
  if (existingAsset) {
    throw new ApiError(
      409,
      "This asset has already been registered",
      "CONFLICT",
    );
  }

  return assetRepository.createAsset({
    productId,
    assetType: payload.assetType,
    s3Key: payload.s3Key,
    s3Url: buildS3Url(payload.s3Key),
    fileName: payload.fileName,
    fileSize: payload.fileSize || null,
    mimeType: payload.mimeType,
    metadata: buildMetadata(payload),
  });
};

const uploadAsset = async (productId, firebaseUid, payload) => {
  await productService.assertProductOwnership(productId, firebaseUid);
  assertAssetPayloadIsValid(payload, "session");

  const fileBuffer = Buffer.from(payload.fileContentBase64, "base64");
  if (!fileBuffer.length) {
    throw new ApiError(
      400,
      "fileContentBase64 must contain a valid base64 payload",
      "BAD_REQUEST",
    );
  }

  const key = generateS3Key(
    String(productId),
    payload.assetType,
    payload.fileName,
  );
  const uploadResult = await uploadToS3(fileBuffer, key, payload.mimeType);

  return assetRepository.createAsset({
    productId,
    assetType: payload.assetType,
    s3Key: uploadResult.key,
    s3Url: uploadResult.location,
    fileName: payload.fileName,
    fileSize: payload.fileSize || fileBuffer.length,
    mimeType: payload.mimeType,
    metadata: buildMetadata(payload),
  });
};

const withSignedAccessUrl = async (asset) => {
  const accessUrl = await generateSignedUrl(
    asset.s3Key,
    DOWNLOAD_URL_EXPIRY_SECONDS,
  );
  return {
    ...asset,
    accessUrl,
  };
};

const getProductAssets = async (productId, firebaseUid) => {
  await productService.assertProductOwnership(productId, firebaseUid);
  const assets = await assetRepository.findByProductId(productId);
  const signedAssets = await Promise.all(assets.map(withSignedAccessUrl));

  const grouped = signedAssets.reduce((acc, a) => {
    const key = a.assetType + "s";
    if (!acc[key]) acc[key] = [];
    acc[key].push(a);
    return acc;
  }, {});

  return grouped;
};

module.exports = {
  createUploadSession,
  confirmAssetUpload,
  uploadAsset,
  getProductAssets,
};
