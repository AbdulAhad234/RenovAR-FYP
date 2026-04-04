const userRepository = require('../users/repository');

module.exports = {
  findUserByFirebaseUid: userRepository.findByFirebaseUid,
  upsertUser: userRepository.upsertUser
};
