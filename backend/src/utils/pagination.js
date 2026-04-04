const VALIDATION_RULES = require('../constants/validationRules');

const getPagination = (query) => {
  const page = Math.max(parseInt(query.page, 10) || VALIDATION_RULES.DEFAULT_PAGE, 1);
  const requestedLimit = parseInt(query.limit, 10) || VALIDATION_RULES.DEFAULT_LIMIT;
  const limit = Math.min(Math.max(requestedLimit, 1), VALIDATION_RULES.MAX_LIMIT);
  const offset = (page - 1) * limit;

  return {
    page,
    limit,
    offset
  };
};

module.exports = {
  getPagination
};
