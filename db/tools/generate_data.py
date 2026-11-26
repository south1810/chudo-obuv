"""Импорт данных заказчика в схему по методичке (роли, справочники, товары, заказы)."""
import os
import re
import datetime
import openpyxl

BASE = r"C:\Users\Admin\Desktop\session_1_kim"
OUT = r"C:\Users\Admin\Desktop\chudo_obuv_wpf\db\02_data.sql"

problems = []


def q(value):
    if value is None:
        return "NULL"
    return "'" + str(value).replace("\\", "\\\\").replace("'", "''") + "'"


def s(value):
    if value is None:
        return None
    return re.sub(r"\s+", " ", str(value)).strip() or None


def norm(value):
    return re.sub(r"\s+", " ", (value or "").replace("ё", "е").replace("Ё", "Е")).strip().lower()


def rows_of(path):
    wb = openpyxl.load_workbook(path, data_only=True)
    ws = wb.worksheets[0]
    data = []
    for row in ws.iter_rows(values_only=True):
        if all(c is None or str(c).strip() == "" for c in row):
            continue
        data.append(row)
    wb.close()
    return data[0], data[1:]


def check(condition, message):
    if not condition:
        problems.append(message)


# ---------------------------------------------------------------- пользователи
_, rows = rows_of(os.path.join(BASE, "Users_import.xlsx"))
users = []
for r in rows:
    users.append({
        "last": s(r[0]), "first": s(r[1]), "middle": s(r[2]),
        "login": s(r[3]), "role": s(r[4]),
    })

roles = ["Администратор", "Менеджер", "Авторизованный пользователь"]
for u in users:
    check(u["role"] in roles, f"неизвестная роль: {u['role']}")
check(len({u["login"] for u in users}) == len(users), "дублируются логины")
users_by_login = {u["login"]: i for i, u in enumerate(users, 1)}
users_by_fio = {f"{u['last']} {u['first']} {u['middle']}": i for i, u in enumerate(users, 1)}

# ---------------------------------------------------------------- товары
_, rows = rows_of(os.path.join(BASE, "Products_import.xlsx"))
categories, subcategories, manufacturers = [], [], []
products = []
for r in rows:
    category, subcategory, image, name, brand, description, composition, price = (
        s(r[0]), s(r[1]), s(r[2]), s(r[3]), s(r[4]), s(r[5]), s(r[6]), r[7])
    if category not in categories:
        categories.append(category)
    if subcategory not in subcategories:
        subcategories.append(subcategory)
    if brand not in manufacturers:
        manufacturers.append(brand)
    products.append({
        "category": category, "subcategory": subcategory, "image": image,
        "name": name, "brand": brand, "description": description,
        "composition": composition, "price": float(price),
    })

check(len(products) == len({(p["name"], p["brand"]) for p in products}),
      "дублируются пары «наименование + производитель»")

products_by_name = {}
for index, product in enumerate(products, 1):
    products_by_name[product["name"]] = index
products_by_norm = {norm(name): index for name, index in products_by_name.items()}


def resolve_product(name):
    if name in products_by_name:
        return products_by_name[name]
    key = norm(name)
    if key in products_by_norm:
        return products_by_norm[key]
    candidates = [index for nkey, index in products_by_norm.items()
                  if nkey.startswith(key) or key.startswith(nkey)]
    return candidates[0] if len(candidates) == 1 else None


# ---------------------------------------------------------------- размеры
_, rows = rows_of(os.path.join(BASE, "Sizes_import.xlsx"))
sizes = sorted({float(r[0]) for r in rows if r[0] is not None})
size_index = {value: i for i, value in enumerate(sizes, 1)}

# ---------------------------------------------------------------- остатки
_, rows = rows_of(os.path.join(BASE, "Stock_Items_import.xlsx"))
stock = []
for r in rows:
    name, brand, size, quantity = s(r[0]), s(r[1]), r[2], r[3]
    product_index = resolve_product(name)
    check(product_index is not None, f"товар отсутствует в каталоге: {name!r}")
    size = float(size)
    check(size in size_index, f"размер отсутствует: {size}")
    if product_index is not None:
        check(products[product_index - 1]["brand"] == brand,
              f"не совпадает производство у {name!r}")
        stock.append({"product": product_index, "size": size, "quantity": int(quantity)})

stock_keys = {(item["product"], item["size"]): index
              for index, item in enumerate(stock, 1)}
check(len(stock_keys) == len(stock), "дублируются товарные позиции")

# ---------------------------------------------------------------- заказы
_, rows = rows_of(os.path.join(BASE, "Orders_import.xlsx"))
orders = {}
for r in rows:
    number, date, fio, category, name, brand, size, quantity, price = (
        r[0], r[1], s(r[2]), s(r[3]), s(r[4]), s(r[5]), r[6], r[7], r[8])
    number = int(number)
    check(fio in users_by_fio, f"клиент отсутствует среди пользователей: {fio!r}")
    product_index = resolve_product(name)
    check(product_index is not None, f"товар отсутствует: {name!r}")
    size = float(size)
    key = (product_index, size)
    check(key in stock_keys, f"позиции нет в наличии: {name!r}, размер {size}")

    if number not in orders:
        orders[number] = {"date": date, "fio": fio, "items": []}
    else:
        check(orders[number]["fio"] == fio, f"заказ {number}: разные клиенты")

    orders[number]["items"].append({
        "stock_item": stock_keys[key],
        "quantity": int(quantity),
        "price": float(price),
        "name": name,
    })

if problems:
    print("НАЙДЕНЫ ПРОБЛЕМЫ:")
    for p in problems[:25]:
        print("  -", p)
    raise SystemExit("импорт остановлен")

# ---------------------------------------------------------------- SQL
lines = []
add = lines.append
add("-- =====================================================================")
add("--  Импорт данных заказчика в схему isp-45_dolgih_ka_chudo_obuv")
add("--  Источники: Users_import.xlsx, Products_import.xlsx, Sizes_import.xlsx,")
add("--             Stock_Items_import.xlsx, Orders_import.xlsx")
add("-- =====================================================================")
add("USE `isp-45_dolgih_ka_chudo_obuv`;")
add("START TRANSACTION;")
add("")

add("INSERT INTO roles (role_id, role_name) VALUES")
add(",\n".join(f"    ({i + 1}, {q(n)})" for i, n in enumerate(roles)) + ";")
add("")

add("INSERT INTO categories (category_id, category_name) VALUES")
add(",\n".join(f"    ({i + 1}, {q(n)})" for i, n in enumerate(categories)) + ";")
add("")

add("INSERT INTO subcategories (subcategory_id, subcategory_name) VALUES")
add(",\n".join(f"    ({i + 1}, {q(n)})" for i, n in enumerate(subcategories)) + ";")
add("")

add("INSERT INTO manufacturers (manufacturer_id, manufacturer_name) VALUES")
add(",\n".join(f"    ({i + 1}, {q(n)})" for i, n in enumerate(manufacturers)) + ";")
add("")

add("INSERT INTO sizes (size_id, size_value) VALUES")
add(",\n".join(f"    ({i}, {value:.1f})" for value, i in sorted(size_index.items(), key=lambda kv: kv[1])) + ";")
add("")

add("INSERT INTO users (user_id, last_name, first_name, patronymic, login, role_id) VALUES")
values = []
for i, u in enumerate(users, 1):
    values.append(f"    ({i}, {q(u['last'])}, {q(u['first'])}, {q(u['middle'])}, "
                  f"{q(u['login'])}, {roles.index(u['role']) + 1})")
add(",\n".join(values) + ";")
add("")

add("INSERT INTO products (product_id, product_name, category_id, subcategory_id,")
add("                     manufacturer_id, description, composition, price, image_file) VALUES")
values = []
for i, p in enumerate(products, 1):
    values.append(
        f"    ({i}, {q(p['name'])}, {categories.index(p['category']) + 1}, "
        f"{subcategories.index(p['subcategory']) + 1}, {manufacturers.index(p['brand']) + 1}, "
        f"{q(p['description'])}, {q(p['composition'])}, {p['price']:.2f}, {q(p['image'])})")
add(",\n".join(values) + ";")
add("")

add("INSERT INTO stock_items (stock_item_id, product_id, size_id, quantity) VALUES")
values = []
for i, item in enumerate(stock, 1):
    values.append(f"    ({i}, {item['product']}, {size_index[item['size']]}, {item['quantity']})")
add(",\n".join(values) + ";")
add("")

add("INSERT INTO orders (order_id, order_date, user_id) VALUES")
values = []
for number in sorted(orders):
    date = orders[number]["date"]
    date = date.strftime("%Y-%m-%d") if isinstance(date, (datetime.datetime, datetime.date)) else str(date)[:10]
    values.append(f"    ({number}, '{date}', {users_by_fio[orders[number]['fio']]})")
add(",\n".join(values) + ";")
add("")

add("INSERT INTO order_items (order_id, stock_item_id, quantity, unit_price) VALUES")
values = []
for number in sorted(orders):
    for item in orders[number]["items"]:
        values.append(f"    ({number}, {item['stock_item']}, {item['quantity']}, {item['price']:.2f})")
add(",\n".join(values) + ";")
add("")

add("COMMIT;")
add("")
add("SELECT 'roles' AS entity, COUNT(*) AS rows_loaded FROM roles")
add("UNION ALL SELECT 'users', COUNT(*) FROM users")
add("UNION ALL SELECT 'categories', COUNT(*) FROM categories")
add("UNION ALL SELECT 'subcategories', COUNT(*) FROM subcategories")
add("UNION ALL SELECT 'manufacturers', COUNT(*) FROM manufacturers")
add("UNION ALL SELECT 'sizes', COUNT(*) FROM sizes")
add("UNION ALL SELECT 'products', COUNT(*) FROM products")
add("UNION ALL SELECT 'stock_items', COUNT(*) FROM stock_items")
add("UNION ALL SELECT 'orders', COUNT(*) FROM orders")
add("UNION ALL SELECT 'order_items', COUNT(*) FROM order_items;")

with open(OUT, "w", encoding="utf-8", newline="\n") as f:
    f.write("\n".join(lines) + "\n")

print("пользователи:", len(users), "| роли:", roles)
print("категории:", categories, "| подкатегории:", len(subcategories), "| производители:", len(manufacturers))
print("товары:", len(products), "| размеры:", len(sizes), f"({sizes[0]}..{sizes[-1]})")
print("товарные позиции:", len(stock), "| пар всего:", sum(i["quantity"] for i in stock))
print("заказы:", len(orders), "| строк:", sum(len(o["items"]) for o in orders.values()))
print("SQL:", OUT)
