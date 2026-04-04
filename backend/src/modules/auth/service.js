const authRepository = require('./repository');
const { getAuth } = require('../../utils/tokenHelper');
const { ApiError } = require('../../utils/apiError');

const mapFirebaseUser = (decodedUser, requestedRole) => ({
  firebaseUid: decodedUser.uid,
  email: decodedUser.email,
  displayName: decodedUser.name || decodedUser.display_name || null,
  profilePicUrl: decodedUser.picture || null,
  role: requestedRole || decodedUser.role || 'customer',
  emailVerified: Boolean(decodedUser.email_verified)
});

const syncAuthenticatedUser = async (authUser, requestedRole) => {
  if (!authUser || !authUser.uid || !authUser.email) {
    throw new ApiError(400, 'Authenticated user payload is incomplete', 'BAD_REQUEST');
  }

  return authRepository.upsertUser(mapFirebaseUser(authUser, requestedRole));
};

const getCurrentUser = async (firebaseUid) => {
  const user = await authRepository.findUserByFirebaseUid(firebaseUid);

  if (!user) {
    const firebaseUser = await getAuth().getUser(firebaseUid);
    return syncAuthenticatedUser({
      uid: firebaseUser.uid,
      email: firebaseUser.email,
      name: firebaseUser.displayName,
      picture: firebaseUser.photoURL,
      email_verified: firebaseUser.emailVerified
    });
  }

  return user;
};

module.exports = {
  syncAuthenticatedUser,
  getCurrentUser
};
