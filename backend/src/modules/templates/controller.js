const { successResponse } = require("../../utils/responseFormatter");
const templateService = require("./service");

const createTemplate = async (req, res) => {
  const template = await templateService.createTemplate(req.user.uid, req.body);
  return successResponse(res, template, "Template created successfully", 201);
};

const getUserTemplates = async (req, res) => {
  const templates = await templateService.getUserTemplates(req.user.uid);
  return successResponse(res, templates, "Templates fetched successfully");
};

const updateTemplate = async (req, res) => {
  const template = await templateService.updateTemplate(
    req.params.templateId,
    req.user.uid,
    req.body,
  );
  return successResponse(res, template, "Template updated successfully");
};

const deleteTemplate = async (req, res) => {
  const result = await templateService.deleteTemplate(
    req.params.templateId,
    req.user.uid,
  );
  return successResponse(res, result, "Template deleted successfully");
};

const shareTemplate = async (req, res) => {
  const template = await templateService.shareTemplate(
    req.params.templateId,
    req.user.uid,
  );
  return successResponse(res, template, "Template shared successfully");
};

module.exports = {
  createTemplate,
  getUserTemplates,
  updateTemplate,
  deleteTemplate,
  shareTemplate,
};
