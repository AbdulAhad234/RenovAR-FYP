ALTER TABLE products
ADD COLUMN IF NOT EXISTS brand_name VARCHAR(255);

UPDATE products p
SET brand_name = COALESCE(vp.company_name, u.display_name, u.email)
FROM users u
LEFT JOIN vendor_profiles vp ON vp.user_id = u.id
WHERE p.vendor_id = u.id
  AND (p.brand_name IS NULL OR BTRIM(p.brand_name) = '');

CREATE INDEX IF NOT EXISTS idx_products_brand_name ON products (LOWER(brand_name));
