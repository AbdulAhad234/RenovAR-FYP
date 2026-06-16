ALTER TABLE products
ADD COLUMN IF NOT EXISTS texture_target VARCHAR(20);

ALTER TABLE products
DROP CONSTRAINT IF EXISTS products_texture_target_check;

ALTER TABLE products
ADD CONSTRAINT products_texture_target_check CHECK (
    texture_target IS NULL OR texture_target IN ('floor', 'wall', 'both')
);

UPDATE products p
SET texture_target = 'wall'
FROM categories c
WHERE p.category_id = c.id
  AND c.slug = 'wall-textures'
  AND p.texture_target IS NULL;

UPDATE products p
SET texture_target = 'floor'
FROM categories c
WHERE p.category_id = c.id
  AND c.slug IN ('flooring', 'floors', 'floor-textures')
  AND p.texture_target IS NULL;

CREATE INDEX IF NOT EXISTS idx_products_texture_target ON products(texture_target);
