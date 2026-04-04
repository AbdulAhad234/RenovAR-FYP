const templateRepository = require('./repository');
const { ApiError } = require('../../utils/apiError');

const assertOwnership = async (templateId, ownerUid) => {
  const template = await templateRepository.getTemplateById(templateId);

  if (!template) {
    throw new ApiError(404, 'Template not found', 'NOT_FOUND');
  }

  if (template.ownerUid !== ownerUid) {
    throw new ApiError(403, 'Access forbidden', 'FORBIDDEN');
  }

  return template;
};

const createTemplate = async (firebaseUid, payload) => {
  return templateRepository.createTemplate({
    ownerUid: firebaseUid,
    name: payload.name,
    roomType: payload.roomType,
    roomLayout: payload.roomLayout,
    placedObjects: payload.placedObjects
  });
};

const getUserTemplates = async (firebaseUid) => templateRepository.getUserTemplates(firebaseUid);

const updateTemplate = async (templateId, firebaseUid, payload) => {
  await assertOwnership(templateId, firebaseUid);
  return templateRepository.updateTemplate(templateId, payload);
};

const deleteTemplate = async (templateId, firebaseUid) => {
  await assertOwnership(templateId, firebaseUid);
  await templateRepository.deleteTemplate(templateId);
  return { templateId };
};

const shareTemplate = async (templateId, firebaseUid) => {
  await assertOwnership(templateId, firebaseUid);
  return templateRepository.updateTemplate(templateId, { shared: true });
};

module.exports = {
  createTemplate,
  getUserTemplates,
  updateTemplate,
  deleteTemplate,
  shareTemplate
};
