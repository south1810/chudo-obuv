using ChudoObuv.Logic;
using ChudoObuv.Models;
using MySqlConnector;

namespace ChudoObuv.Data;

/// <summary>Все запросы приложения к базе данных.</summary>
public static class DataService
{
    // ---------------------------- Пользователи ----------------------------

    public static User? FindUserByLogin(string login)
    {
        const string sql = """
            SELECT u.user_id, u.last_name, u.first_name, u.patronymic, u.login, r.role_name
            FROM users u
                     JOIN roles r ON r.role_id = u.role_id
            WHERE u.login = @login
            """;

        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@login", login.Trim());
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadUser(reader) : null;
    }

    public static List<User> GetClients()
    {
        const string sql = """
            SELECT u.user_id, u.last_name, u.first_name, u.patronymic, u.login, r.role_name
            FROM users u
                     JOIN roles r ON r.role_id = u.role_id
            WHERE r.role_name = @role
            ORDER BY u.last_name, u.first_name
            """;

        var result = new List<User>();
        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@role", User.ClientRole);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(ReadUser(reader));
        }

        return result;
    }

    private static User ReadUser(MySqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(0),
        LastName = reader.GetString(1),
        FirstName = reader.GetString(2),
        Patronymic = reader.IsDBNull(3) ? null : reader.GetString(3),
        Login = reader.GetString(4),
        RoleName = reader.GetString(5)
    };

    // ---------------------------- Каталог ----------------------------

    public static List<string> GetCategories()
    {
        var result = new List<string>();
        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand(
            "SELECT category_name FROM categories ORDER BY category_name", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    /// <summary>
    /// Товары каталога с размерами и остатками.
    /// Признак «были заказы в предыдущем календарном месяце» вычисляется на стороне СУБД.
    /// </summary>
    public static List<Product> GetProducts(DateTime calculationDate)
    {
        var currentMonthStart = PriceCalculator.GetCurrentMonthStart(calculationDate);
        var previousMonthStart = PriceCalculator.GetPreviousMonthStart(calculationDate);

        const string sql = """
            SELECT v.product_id, v.product_name, v.category_id, v.category_name,
                   v.subcategory_name, v.manufacturer_name, v.description, v.composition,
                   v.price, v.image_file, v.total_quantity,
                   EXISTS(SELECT 1
                          FROM order_items oi
                                   JOIN orders o ON o.order_id = oi.order_id
                                   JOIN stock_items si ON si.stock_item_id = oi.stock_item_id
                          WHERE si.product_id = v.product_id
                            AND o.order_date >= @previousStart
                            AND o.order_date < @currentStart) AS has_orders
            FROM v_products v
            ORDER BY v.product_id
            """;

        var products = new List<Product>();
        using var connection = Database.OpenConnection();
        using (var command = new MySqlCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@previousStart", previousMonthStart);
            command.Parameters.AddWithValue("@currentStart", currentMonthStart);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new Product
                {
                    ProductId = reader.GetInt32(0),
                    ProductName = reader.GetString(1),
                    CategoryId = reader.GetInt32(2),
                    CategoryName = reader.GetString(3),
                    SubcategoryName = reader.GetString(4),
                    ManufacturerName = reader.GetString(5),
                    Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Composition = reader.IsDBNull(7) ? null : reader.GetString(7),
                    Price = reader.GetDecimal(8),
                    ImageFile = reader.IsDBNull(9) ? null : reader.GetString(9),
                    TotalQuantity = Convert.ToInt32(reader.GetValue(10)),
                    HasOrdersInPreviousMonth = Convert.ToBoolean(reader.GetValue(11))
                });
            }
        }

        const string sizesSql = """
            SELECT si.stock_item_id, si.product_id, sz.size_value, si.quantity
            FROM stock_items si
                     JOIN sizes sz ON sz.size_id = si.size_id
            WHERE si.quantity > 0
            ORDER BY si.product_id, sz.size_value
            """;

        var sizes = products.ToDictionary(p => p.ProductId, _ => new List<SizeStock>());
        using (var command = new MySqlCommand(sizesSql, connection))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var productId = reader.GetInt32(1);
                if (sizes.TryGetValue(productId, out var list))
                {
                    list.Add(new SizeStock
                    {
                        StockItemId = reader.GetInt32(0),
                        SizeValue = reader.GetDecimal(2),
                        Quantity = reader.GetInt32(3)
                    });
                }
            }
        }

        foreach (var product in products)
        {
            product.Sizes.AddRange(sizes[product.ProductId]);
        }

        return products;
    }

    // ---------------------------- Заказы ----------------------------

    public static List<OrderRow> GetOrders()
    {
        const string sql = """
            SELECT order_id, order_date, user_id, client_name, position_count, items_count, total_cost
            FROM v_orders
            ORDER BY order_id
            """;

        var result = new List<OrderRow>();
        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand(sql, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new OrderRow
            {
                OrderId = reader.GetInt32(0),
                OrderDate = reader.GetDateTime(1),
                UserId = reader.GetInt32(2),
                ClientName = reader.GetString(3),
                PositionCount = Convert.ToInt32(reader.GetValue(4)),
                ItemsCount = Convert.ToInt32(reader.GetValue(5)),
                TotalCost = reader.GetDecimal(6)
            });
        }

        return result;
    }

    public static List<OrderLine> GetOrderLines(int orderId)
    {
        const string sql = """
            SELECT order_item_id, stock_item_id, product_name, manufacturer_name,
                   size_value, quantity, unit_price
            FROM v_order_items
            WHERE order_id = @orderId
            ORDER BY order_item_id
            """;

        var result = new List<OrderLine>();
        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@orderId", orderId);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new OrderLine
            {
                OrderItemId = reader.GetInt32(0),
                StockItemId = reader.GetInt32(1),
                ProductName = reader.GetString(2),
                ManufacturerName = reader.GetString(3),
                SizeValue = reader.GetDecimal(4),
                Quantity = reader.GetInt32(5),
                UnitPrice = reader.GetDecimal(6)
            });
        }

        return result;
    }

    /// <summary>Создание заказа. Номер выдаётся автоматически (+1 к максимальному).</summary>
    public static int CreateOrder(int userId, DateTime orderDate, IEnumerable<CartLine> lines)
    {
        var items = lines.Where(line => line.Quantity > 0).ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Заказ не может быть пустым.");
        }

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        var orderId = Convert.ToInt32(new MySqlCommand(
            "SELECT COALESCE(MAX(order_id), 0) + 1 FROM orders FOR UPDATE",
            connection, transaction).ExecuteScalar());

        using (var command = new MySqlCommand(
                   "INSERT INTO orders (order_id, order_date, user_id) VALUES (@id, @date, @user)",
                   connection, transaction))
        {
            command.Parameters.AddWithValue("@id", orderId);
            command.Parameters.AddWithValue("@date", orderDate.Date);
            command.Parameters.AddWithValue("@user", userId);
            command.ExecuteNonQuery();
        }

        InsertItems(connection, transaction, orderId, items);
        transaction.Commit();
        return orderId;
    }

    /// <summary>Изменение существующего заказа (администратор).</summary>
    public static void UpdateOrder(int orderId, int userId, DateTime orderDate, IEnumerable<CartLine> lines)
    {
        var items = lines.Where(line => line.Quantity > 0).ToList();
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Заказ не может быть пустым.");
        }

        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var command = new MySqlCommand(
                   "UPDATE orders SET order_date = @date, user_id = @user WHERE order_id = @id",
                   connection, transaction))
        {
            command.Parameters.AddWithValue("@date", orderDate.Date);
            command.Parameters.AddWithValue("@user", userId);
            command.Parameters.AddWithValue("@id", orderId);
            command.ExecuteNonQuery();
        }

        using (var command = new MySqlCommand(
                   "DELETE FROM order_items WHERE order_id = @id", connection, transaction))
        {
            command.Parameters.AddWithValue("@id", orderId);
            command.ExecuteNonQuery();
        }

        InsertItems(connection, transaction, orderId, items);
        transaction.Commit();
    }

    public static void DeleteOrder(int orderId)
    {
        using var connection = Database.OpenConnection();
        using var command = new MySqlCommand("DELETE FROM orders WHERE order_id = @id", connection);
        command.Parameters.AddWithValue("@id", orderId);
        command.ExecuteNonQuery();
    }

    private static void InsertItems(MySqlConnection connection, MySqlTransaction transaction,
        int orderId, IEnumerable<CartLine> items)
    {
        foreach (var line in items)
        {
            using var command = new MySqlCommand("""
                INSERT INTO order_items (order_id, stock_item_id, quantity, unit_price)
                VALUES (@order, @stock, @quantity, @price)
                """, connection, transaction);
            command.Parameters.AddWithValue("@order", orderId);
            command.Parameters.AddWithValue("@stock", line.StockItemId);
            command.Parameters.AddWithValue("@quantity", line.Quantity);
            command.Parameters.AddWithValue("@price", line.UnitPrice);
            command.ExecuteNonQuery();
        }
    }
}
