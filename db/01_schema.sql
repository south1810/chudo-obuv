-- =====================================================================
--  Система оформления заказа обуви «Чудо Обувь»
--  Демонстрационный экзамен, КИМ 09.02.07-2-2027
--  СУБД: MySQL 8.0, схема приведена к третьей нормальной форме (3НФ)
--  Схема: isp-45_dolgih_ka_chudo_obuv
-- =====================================================================

DROP DATABASE IF EXISTS `isp-45_dolgih_ka_chudo_obuv`;
CREATE DATABASE `isp-45_dolgih_ka_chudo_obuv`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;
USE `isp-45_dolgih_ka_chudo_obuv`;

-- ---------------------------------------------------------------------
-- Справочник ролей
-- ---------------------------------------------------------------------
CREATE TABLE roles (
    role_id   INT         NOT NULL AUTO_INCREMENT,
    role_name VARCHAR(50) NOT NULL,
    PRIMARY KEY (role_id),
    UNIQUE KEY role_name_UNIQUE (role_name)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Справочник категорий
-- ---------------------------------------------------------------------
CREATE TABLE categories (
    category_id   INT          NOT NULL AUTO_INCREMENT,
    category_name VARCHAR(100) NOT NULL,
    PRIMARY KEY (category_id),
    UNIQUE KEY category_name_UNIQUE (category_name)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Справочник подкатегорий
-- ---------------------------------------------------------------------
CREATE TABLE subcategories (
    subcategory_id   INT          NOT NULL AUTO_INCREMENT,
    subcategory_name VARCHAR(100) NOT NULL,
    PRIMARY KEY (subcategory_id),
    UNIQUE KEY subcategory_name_UNIQUE (subcategory_name)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Справочник производителей
-- ---------------------------------------------------------------------
CREATE TABLE manufacturers (
    manufacturer_id   INT         NOT NULL AUTO_INCREMENT,
    manufacturer_name VARCHAR(100) NOT NULL,
    PRIMARY KEY (manufacturer_id),
    UNIQUE KEY manufacturer_name_UNIQUE (manufacturer_name)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Справочник размеров
-- ---------------------------------------------------------------------
CREATE TABLE sizes (
    size_id    INT          NOT NULL AUTO_INCREMENT,
    size_value DECIMAL(3,1) NOT NULL,
    PRIMARY KEY (size_id),
    UNIQUE KEY size_value_UNIQUE (size_value)
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Пользователи системы (вход по логину, без пароля)
-- ---------------------------------------------------------------------
CREATE TABLE users (
    user_id    INT         NOT NULL AUTO_INCREMENT,
    last_name  VARCHAR(50) NOT NULL,
    first_name VARCHAR(50) NOT NULL,
    patronymic VARCHAR(50) NULL,
    login      VARCHAR(50) NOT NULL,
    role_id    INT         NOT NULL,
    PRIMARY KEY (user_id),
    UNIQUE KEY login_UNIQUE (login),
    KEY ix_users_role (role_id),
    CONSTRAINT fk_users_roles FOREIGN KEY (role_id)
        REFERENCES roles (role_id) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Товары (модели обуви)
-- ---------------------------------------------------------------------
CREATE TABLE products (
    product_id      INT           NOT NULL AUTO_INCREMENT,
    product_name    VARCHAR(150)  NOT NULL,
    category_id     INT           NOT NULL,
    subcategory_id  INT           NOT NULL,
    manufacturer_id INT           NOT NULL,
    description     TEXT          NULL,
    composition     TEXT          NULL,
    price           DECIMAL(10, 2) NOT NULL,
    image_file      VARCHAR(100)  NULL,
    PRIMARY KEY (product_id),
    UNIQUE KEY uq_products_name_manufacturer (product_name, manufacturer_id),
    KEY ix_products_category (category_id),
    KEY ix_products_subcategory (subcategory_id),
    KEY ix_products_manufacturer (manufacturer_id),
    CONSTRAINT chk_products_price CHECK (price >= 0),
    CONSTRAINT fk_products_categories FOREIGN KEY (category_id)
        REFERENCES categories (category_id) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_products_subcategories FOREIGN KEY (subcategory_id)
        REFERENCES subcategories (subcategory_id) ON UPDATE CASCADE ON DELETE RESTRICT,
    CONSTRAINT fk_products_manufacturers FOREIGN KEY (manufacturer_id)
        REFERENCES manufacturers (manufacturer_id) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Товарные позиции: модель + размер + доступное количество
-- ---------------------------------------------------------------------
CREATE TABLE stock_items (
    stock_item_id INT NOT NULL AUTO_INCREMENT,
    product_id    INT NOT NULL,
    size_id       INT NOT NULL,
    quantity      INT NOT NULL DEFAULT 0,
    PRIMARY KEY (stock_item_id),
    UNIQUE KEY uq_stock_items_product_size (product_id, size_id),
    KEY ix_stock_items_size (size_id),
    CONSTRAINT chk_stock_items_quantity CHECK (quantity >= 0),
    CONSTRAINT fk_stock_items_products FOREIGN KEY (product_id)
        REFERENCES products (product_id) ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_stock_items_sizes FOREIGN KEY (size_id)
        REFERENCES sizes (size_id) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Заказы (шапка)
-- ---------------------------------------------------------------------
CREATE TABLE orders (
    order_id   INT  NOT NULL AUTO_INCREMENT,
    order_date DATE NOT NULL,
    user_id    INT  NOT NULL,
    PRIMARY KEY (order_id),
    KEY ix_orders_user (user_id),
    KEY ix_orders_date (order_date),
    CONSTRAINT fk_orders_users FOREIGN KEY (user_id)
        REFERENCES users (user_id) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Строки заказа
-- ---------------------------------------------------------------------
CREATE TABLE order_items (
    order_item_id INT           NOT NULL AUTO_INCREMENT,
    order_id      INT           NOT NULL,
    stock_item_id INT           NOT NULL,
    quantity      INT           NOT NULL,
    unit_price    DECIMAL(10, 2) NOT NULL,
    PRIMARY KEY (order_item_id),
    UNIQUE KEY uq_order_items_order_stock (order_id, stock_item_id),
    KEY ix_order_items_stock (stock_item_id),
    CONSTRAINT chk_order_items_qty CHECK (quantity > 0),
    CONSTRAINT chk_order_items_price CHECK (unit_price >= 0),
    CONSTRAINT fk_order_items_orders FOREIGN KEY (order_id)
        REFERENCES orders (order_id) ON UPDATE CASCADE ON DELETE CASCADE,
    CONSTRAINT fk_order_items_stock_items FOREIGN KEY (stock_item_id)
        REFERENCES stock_items (stock_item_id) ON UPDATE CASCADE ON DELETE RESTRICT
) ENGINE = InnoDB;

-- ---------------------------------------------------------------------
-- Представление: карточка товара с остатком и категориями
-- ---------------------------------------------------------------------
CREATE VIEW v_products AS
SELECT p.product_id,
       p.product_name,
       p.price,
       p.description,
       p.composition,
       p.image_file,
       c.category_id,
       c.category_name,
       s.subcategory_id,
       s.subcategory_name,
       m.manufacturer_id,
       m.manufacturer_name,
       COALESCE((SELECT SUM(si.quantity) FROM stock_items si WHERE si.product_id = p.product_id), 0)
           AS total_quantity
FROM products p
         JOIN categories c ON c.category_id = p.category_id
         JOIN subcategories s ON s.subcategory_id = p.subcategory_id
         JOIN manufacturers m ON m.manufacturer_id = p.manufacturer_id;

-- ---------------------------------------------------------------------
-- Представление: сводка по заказам
-- ---------------------------------------------------------------------
CREATE VIEW v_orders AS
SELECT o.order_id,
       o.order_date,
       u.user_id,
       CONCAT_WS(' ', u.last_name, u.first_name, u.patronymic) AS client_name,
       COUNT(oi.order_item_id)                                 AS position_count,
       COALESCE(SUM(oi.quantity), 0)                           AS items_count,
       ROUND(COALESCE(SUM(oi.quantity * oi.unit_price), 0), 2) AS total_cost
FROM orders o
         JOIN users u ON u.user_id = o.user_id
         LEFT JOIN order_items oi ON oi.order_id = o.order_id
GROUP BY o.order_id, o.order_date, u.user_id, u.last_name, u.first_name, u.patronymic;

-- ---------------------------------------------------------------------
-- Представление: состав заказа
-- ---------------------------------------------------------------------
CREATE VIEW v_order_items AS
SELECT oi.order_item_id,
       oi.order_id,
       oi.stock_item_id,
       p.product_id,
       p.product_name,
       m.manufacturer_name,
       sz.size_value,
       oi.quantity,
       oi.unit_price,
       ROUND(oi.quantity * oi.unit_price, 2) AS line_cost
FROM order_items oi
         JOIN stock_items si ON si.stock_item_id = oi.stock_item_id
         JOIN products p ON p.product_id = si.product_id
         JOIN manufacturers m ON m.manufacturer_id = p.manufacturer_id
         JOIN sizes sz ON sz.size_id = si.size_id;
