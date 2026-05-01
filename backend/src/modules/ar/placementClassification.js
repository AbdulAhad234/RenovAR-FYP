const DEFAULT_PLACEMENT = Object.freeze({
  anchorType: 'horizontal',
  allowedPlaneClassifications: ['Floor'],
  fallbackPlaneClassification: 'Floor'
});

const CEILING_PLACEMENT = Object.freeze({
  anchorType: 'ceiling',
  allowedPlaneClassifications: ['Ceiling'],
  fallbackPlaneClassification: 'Ceiling'
});

const WALL_PLACEMENT = Object.freeze({
  anchorType: 'vertical',
  allowedPlaneClassifications: ['Wall'],
  fallbackPlaneClassification: 'Wall'
});

const normalize = (value) => String(value || '').trim().toLowerCase();

const getPlacementClassification = (product) => {
  const name = normalize(product.name);
  const categorySlug = normalize(product.categorySlug);

  if (name.includes('pendant') || name.includes('ceiling') || name.includes('light')) {
    return CEILING_PLACEMENT;
  }

  if (name.includes('wall') || name.includes('mirror') || name.includes('painting')) {
    return WALL_PLACEMENT;
  }

  if (
    categorySlug === 'bedroom' ||
    categorySlug === 'dining-room' ||
    categorySlug === 'living-room' ||
    categorySlug === 'decor'
  ) {
    return DEFAULT_PLACEMENT;
  }

  return DEFAULT_PLACEMENT;
};

module.exports = {
  getPlacementClassification
};
