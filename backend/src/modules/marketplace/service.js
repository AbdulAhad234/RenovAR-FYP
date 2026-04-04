const marketplaceRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');
const { paginationResponse } = require('../../utils/responseFormatter');
const { getPagination } = require('../../utils/pagination');
const { generateSignedUrl } = require('../../utils/s3Helper');

const attachSignedAssetUrl = async (asset) => {
  if (!asset.s3Key) {
    return asset;
  }

  return {
    ...asset,
    accessUrl: await generateSignedUrl(asset.s3Key)
  };
};

const searchProducts = async (queryParams) => {
  const pagination = getPagination(queryParams);
  const filters = {
    search: queryParams.search,
    categoryId: queryParams.categoryId ? Number(queryParams.categoryId) : undefined,
    minPrice: queryParams.minPrice !== undefined ? Number(queryParams.minPrice) : undefined,
    maxPrice: queryParams.maxPrice !== undefined ? Number(queryParams.maxPrice) : undefined,
    ...pagination
  };

  const [products, total] = await Promise.all([
    marketplaceRepository.searchApprovedProducts(filters),
    marketplaceRepository.countApprovedProducts(filters)
  ]);

  const productsWithUrls = await Promise.all(
    products.map(async (product) => ({
      ...product,
      thumbnailUrl: product.thumbnailKey ? await generateSignedUrl(product.thumbnailKey) : product.thumbnailUrl
    }))
  );

  return paginationResponse(pagination.page, pagination.limit, total, productsWithUrls);
};

const getProductDetails = async (productId) => {
  const product = await marketplaceRepository.getApprovedProductDetails(productId);

  if (!product) {
    throw new ApiError(404, 'Product not found', 'PRODUCT_NOT_FOUND');
  }

  const assets = await marketplaceRepository.getProductAssets(productId);
  return {
    ...product,
    assets: await Promise.all(assets.map(attachSignedAssetUrl))
  };
};

module.exports = {
  searchProducts,
  getProductDetails
};
