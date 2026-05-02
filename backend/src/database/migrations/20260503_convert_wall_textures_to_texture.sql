BEGIN;

UPDATE product_assets
SET
    asset_type = 'texture'
WHERE
    asset_type = 'image'
    AND metadata_json::text LIKE '%"importSource":"curated-dataset-manifest"%'
    AND product_id IN (
        SELECT
            p.id
        FROM
            products p
            JOIN categories c ON c.id = p.category_id
        WHERE
            c.slug = 'wall-textures'
    );

COMMIT;