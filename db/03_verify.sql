USE `isp-45_dolgih_ka_chudo_obuv`;

SELECT '--- ТАБЛИЦЫ СХЕМЫ ---' AS section;
SELECT TABLE_NAME, TABLE_TYPE, TABLE_ROWS
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'isp-45_dolgih_ka_chudo_obuv'
ORDER BY TABLE_TYPE, TABLE_NAME;

SELECT '--- ПОЛЬЗОВАТЕЛИ И РОЛИ ---' AS section;
SELECT u.user_id, CONCAT_WS(' ', u.last_name, u.first_name, u.patronymic) AS fio,
       u.login, r.role_name
FROM users u JOIN roles r ON r.role_id = u.role_id ORDER BY u.user_id;

SELECT '--- СПРАВОЧНИКИ ---' AS section;
SELECT (SELECT COUNT(*) FROM categories) AS categories,
       (SELECT COUNT(*) FROM subcategories) AS subcategories,
       (SELECT COUNT(*) FROM manufacturers) AS manufacturers,
       (SELECT COUNT(*) FROM sizes) AS sizes,
       (SELECT COUNT(*) FROM roles) AS roles;

SELECT '--- КАТАЛОГ ---' AS section;
SELECT COUNT(*) AS products,
       SUM(total_quantity) AS total_pairs,
       SUM(image_file IS NOT NULL) AS with_photo,
       SUM(total_quantity <= 3) AS low_stock
FROM v_products;

SELECT '--- КАТАЛОГ (первые 8) ---' AS section;
SELECT product_id, product_name, category_name, subcategory_name, manufacturer_name,
       price, total_quantity
FROM v_products ORDER BY product_id LIMIT 8;

SELECT '--- ЗАКАЗЫ ---' AS section;
SELECT order_id, order_date, client_name, position_count, items_count, total_cost
FROM v_orders ORDER BY order_id;

SELECT '--- ЗАКАЗЫ: ИТОГИ ---' AS section;
SELECT COUNT(*) AS orders, SUM(items_count) AS pairs, ROUND(SUM(total_cost), 2) AS revenue
FROM v_orders;

SELECT '--- ПРОВЕРКА ЦЕЛОСТНОСТИ ---' AS section;
SELECT (SELECT COUNT(*) FROM users u LEFT JOIN roles r ON r.role_id=u.role_id WHERE r.role_id IS NULL) AS user_no_role,
       (SELECT COUNT(*) FROM products p LEFT JOIN categories c ON c.category_id=p.category_id WHERE c.category_id IS NULL) AS product_no_category,
       (SELECT COUNT(*) FROM products p LEFT JOIN subcategories s ON s.subcategory_id=p.subcategory_id WHERE s.subcategory_id IS NULL) AS product_no_sub,
       (SELECT COUNT(*) FROM products p LEFT JOIN manufacturers m ON m.manufacturer_id=p.manufacturer_id WHERE m.manufacturer_id IS NULL) AS product_no_manufacturer,
       (SELECT COUNT(*) FROM stock_items si LEFT JOIN products p ON p.product_id=si.product_id WHERE p.product_id IS NULL) AS stock_no_product,
       (SELECT COUNT(*) FROM stock_items si LEFT JOIN sizes s ON s.size_id=si.size_id WHERE s.size_id IS NULL) AS stock_no_size,
       (SELECT COUNT(*) FROM orders o LEFT JOIN users u ON u.user_id=o.user_id WHERE u.user_id IS NULL) AS order_no_user,
       (SELECT COUNT(*) FROM order_items oi LEFT JOIN orders o ON o.order_id=oi.order_id WHERE o.order_id IS NULL) AS item_no_order,
       (SELECT COUNT(*) FROM order_items oi LEFT JOIN stock_items si ON si.stock_item_id=oi.stock_item_id WHERE si.stock_item_id IS NULL) AS item_no_stock;

SELECT '--- ВНЕШНИЕ КЛЮЧИ ---' AS section;
SELECT k.TABLE_NAME, k.CONSTRAINT_NAME, k.REFERENCED_TABLE_NAME, r.DELETE_RULE
FROM information_schema.KEY_COLUMN_USAGE k
         JOIN information_schema.REFERENTIAL_CONSTRAINTS r
              ON r.CONSTRAINT_NAME = k.CONSTRAINT_NAME AND r.CONSTRAINT_SCHEMA = k.TABLE_SCHEMA
WHERE k.TABLE_SCHEMA = 'isp-45_dolgih_ka_chudo_obuv' AND k.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY k.TABLE_NAME, k.CONSTRAINT_NAME;
