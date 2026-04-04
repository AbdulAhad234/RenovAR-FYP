const { getCollection, collections } = require('../../config/firestore');

const collection = () => getCollection(collections.TEMPLATES);

const createTemplate = async (payload) => {
  const docRef = collection().doc();
  const data = {
    id: docRef.id,
    ownerUid: payload.ownerUid,
    name: payload.name,
    roomType: payload.roomType,
    roomLayout: payload.roomLayout,
    placedObjects: payload.placedObjects || [],
    shared: false,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  await docRef.set(data);
  return data;
};

const getUserTemplates = async (ownerUid) => {
  const snapshot = await collection().where('ownerUid', '==', ownerUid).orderBy('updatedAt', 'desc').get();
  return snapshot.docs.map((doc) => doc.data());
};

const getTemplateById = async (templateId) => {
  const doc = await collection().doc(templateId).get();
  return doc.exists ? doc.data() : null;
};

const updateTemplate = async (templateId, payload) => {
  const updateData = {
    ...payload,
    updatedAt: new Date().toISOString()
  };

  await collection().doc(templateId).set(updateData, { merge: true });
  return getTemplateById(templateId);
};

const deleteTemplate = async (templateId) => {
  await collection().doc(templateId).delete();
};

module.exports = {
  createTemplate,
  getUserTemplates,
  getTemplateById,
  updateTemplate,
  deleteTemplate
};
