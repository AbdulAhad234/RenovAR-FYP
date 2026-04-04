const express = require('express');
const { authenticate } = require('../../middlewares/authentication');
const { asyncHandler } = require('../../middlewares/errorHandler');
const { validateRequest } = require('../../middlewares/validation');
const controller = require('./controller');
const { templatePayloadValidation, updateTemplateValidation, templateIdValidation } = require('./validation');

const router = express.Router();

router.post('/', authenticate, validateRequest(templatePayloadValidation), asyncHandler(controller.createTemplate));
router.get('/me', authenticate, asyncHandler(controller.getUserTemplates));
router.patch('/:templateId', authenticate, validateRequest(updateTemplateValidation), asyncHandler(controller.updateTemplate));
router.delete('/:templateId', authenticate, validateRequest(templateIdValidation), asyncHandler(controller.deleteTemplate));
router.post('/:templateId/share', authenticate, validateRequest(templateIdValidation), asyncHandler(controller.shareTemplate));

module.exports = router;
