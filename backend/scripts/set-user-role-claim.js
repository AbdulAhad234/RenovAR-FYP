#!/usr/bin/env node

require('dotenv').config();

const { getAuth, setCustomClaims } = require('../src/utils/tokenHelper');

const VALID_ROLES = new Set(['customer', 'vendor', 'admin']);

const parseArgs = (argv) => {
  const args = {
    uid: null,
    email: null,
    role: null
  };

  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];

    if (arg === '--uid') {
      args.uid = argv[index + 1] || null;
      index += 1;
    } else if (arg === '--email') {
      args.email = argv[index + 1] || null;
      index += 1;
    } else if (arg === '--role') {
      args.role = argv[index + 1] || null;
      index += 1;
    }
  }

  return args;
};

const resolveUid = async ({ uid, email }) => {
  if (uid) {
    return uid;
  }

  if (!email) {
    throw new Error('Provide either --uid or --email');
  }

  const userRecord = await getAuth().getUserByEmail(email);
  return userRecord.uid;
};

const main = async () => {
  const args = parseArgs(process.argv.slice(2));

  if (!args.role || !VALID_ROLES.has(args.role)) {
    throw new Error('Provide a valid --role value: customer, vendor, or admin');
  }

  const uid = await resolveUid(args);
  await setCustomClaims(uid, { role: args.role });

  console.log(`Role claim "${args.role}" set successfully for UID ${uid}`);
  console.log('Ask the user to sign in again and use a fresh Firebase ID token.');
};

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
