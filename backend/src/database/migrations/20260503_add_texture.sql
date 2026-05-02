-- Add 'texture' to allowed asset_type values
ALTER TABLE product_assets
DROP CONSTRAINT IF EXISTS product_assets_asset_type_check;

ALTER TABLE product_assets ADD CONSTRAINT product_assets_asset_type_check CHECK (
    asset_type IN ('image', 'model', 'thumbnail', 'texture')
);

-- Create index on asset_type if not exists (already present in schema.sql, but safe to ensure)
CREATE INDEX IF NOT EXISTS idx_product_assets_asset_type ON product_assets (asset_type);