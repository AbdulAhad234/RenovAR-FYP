const { body } = require('express-validator');
const ROLES = require('../../constants/roles');

const syncUserValidation = [
  body('role')
    .optional()
    .isIn(Object.values(ROLES))
    .withMessage('Role must be customer, vendor, or admin')
];

module.exports = {
  syncUserValidation
};
