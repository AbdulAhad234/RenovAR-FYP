const userRepository = require('./repository');
const { updateFirebaseUser } = require('../../utils/tokenHelper');
const { ApiError } = require('../../utils/apiError');

const getProfileByFirebaseUid = async (firebaseUid) => {
  const user = await userRepository.findByFirebaseUid(firebaseUid);

  if (!user) {
    throw new ApiError(404, 'User not found', 'USER_NOT_FOUND');
  }

  return user;
};

const updateProfileByFirebaseUid = async (firebaseUid, payload) => {
  const user = await getProfileByFirebaseUid(firebaseUid);
  const updatedUser = await userRepository.updateUserProfile(user.id, payload);

  await updateFirebaseUser(firebaseUid, {
    displayName: payload.displayName || user.displayName || undefined,
    photoURL: payload.profilePicUrl || user.profilePicUrl || undefined
  });

  return updatedUser;
};

module.exports = {
  getProfileByFirebaseUid,
  updateProfileByFirebaseUid
};
