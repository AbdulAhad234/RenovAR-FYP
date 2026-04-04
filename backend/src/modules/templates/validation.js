const { body, param } = require('express-validator');

const templatePayloadValidation = [
  body('name').isString().trim().isLength({ min: 2, max: 120 }),
  body('roomType').isString().trim().isLength({ min: 2, max: 100 }),
  body('roomLayout').isObject(),
  body('placedObjects').optional().isArray()
];

const updateTemplateValidation = [
  param('templateId').isString().trim().isLength({ min: 5 }),
  body('name').optional().isString().trim().isLength({ min: 2, max: 120 }),
  body('roomType').optional().isString().trim().isLength({ min: 2, max: 100 }),
  body('roomLayout').optional().isObject(),
  body('placedObjects').optional().isArray()
];

const templateIdValidation = [
  param('templateId').isString().trim().isLength({ min: 5 })
];

module.exports = {
  templatePayloadValidation,
  updateTemplateValidation,
  templateIdValidation
};
