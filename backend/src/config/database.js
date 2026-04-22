// PostgreSQL database configuration
const { Pool } = require('pg');
const { logError } = require('../utils/errorLogger');

const shouldUseSsl = () => {
  const databaseUrl = process.env.DATABASE_URL || '';
  const explicitSsl = String(process.env.DB_SSL || '').toLowerCase();
  const sslMode = String(process.env.PGSSLMODE || '').toLowerCase();

  if (explicitSsl === 'true' || explicitSsl === '1') {
    return true;
  }

  if (explicitSsl === 'false' || explicitSsl === '0') {
    return false;
  }

  if (sslMode === 'require' || sslMode === 'prefer') {
    return true;
  }

  return process.env.NODE_ENV === 'production' || databaseUrl.includes('render.com');
};

// Create connection pool
const pool = new Pool({
  connectionString: process.env.DATABASE_URL,
  ssl: shouldUseSsl() ? { rejectUnauthorized: false } : false,
  max: 20, // Maximum number of clients in the pool
  idleTimeoutMillis: 30000, // Close idle clients after 30 seconds
  connectionTimeoutMillis: 2000, // Return an error after 2 seconds if connection could not be established
});

// Handle pool errors
pool.on('error', (err, client) => {
  logError(err, null, 'Unexpected error on idle client');
});

// Test database connection
const testConnection = async () => {
  try {
    const client = await pool.connect();
    await client.query('SELECT NOW()');
    client.release();
    console.log('✅ PostgreSQL database connected successfully');
    return true;
  } catch (error) {
    logError(error, null, 'PostgreSQL connection test failed');
    return false;
  }
};

// Query helper with error handling
const query = async (text, params) => {
  const start = Date.now();
  try {
    const res = await pool.query(text, params);
    const duration = Date.now() - start;
    console.log(`[${new Date().toISOString()}] Query executed in ${duration}ms: ${text}`);
    return res;
  } catch (error) {
    logError(error, null, `Database query failed: ${text}`);
    throw error;
  }
};

// Transaction helper
const getClient = async () => {
  const client = await pool.connect();
  const query = client.query;
  const release = client.release;

  // Set a timeout of 5 seconds, after which we will log this client's last query
  const timeout = setTimeout(() => {
    console.error('A client has been checked out for more than 5 seconds!');
    console.error(`Last query: ${client.lastQuery}`);
  }, 5000);

  // Monkey patch the query method to keep track of the last query executed
  client.query = (...args) => {
    client.lastQuery = args;
    return query.apply(client, args);
  };

  client.release = () => {
    clearTimeout(timeout);
    // Set the methods back to their old un-monkey-patched version
    client.query = query;
    client.release = release;
    return release.apply(client);
  };

  return client;
};

module.exports = {
  pool,
  query,
  getClient,
  testConnection
};
