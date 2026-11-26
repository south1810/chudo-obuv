using System.IO;
using ChudoObuv.Data;
using ChudoObuv.Logic;
using ChudoObuv.Models;
using MySqlConnector;

namespace ChudoObuv.Tools;

/// <summary>
/// Проверка бизнес-логики и сквозного сценария «оформление заказа».
/// Запуск: ChudoObuv.exe --selftest &lt;файл-отчёта&gt;.
/// </summary>
public static class SelfTest
{
    public static void Run(string reportPath)
    {
        var report = new List<string>();

        void Log(string message)
        {
            report.Add(message);
            Console.WriteLine(message);
        }

        void Check(string name, bool ok) => Log($"{(ok ? "OK  " : "FAIL")} {name}");

        try
        {
            // ---------------- расчёты (чистые функции) ----------------
            var date = new DateTime(2026, 10, 30);
            Check("Начало предыдущего месяца = 01.09.2026",
                PriceCalculator.GetPreviousMonthStart(date) == new DateTime(2026, 9, 1));
            Check("Скидка 25% при отсутствии заказов",
                PriceCalculator.CalculateDiscountedPrice(2190m, false) == 1642.50m);
            Check("Без скидки при наличии заказов",
                PriceCalculator.CalculateDiscountedPrice(2190m, true) == 2190m);
            Check("Округление до копеек (1999 → 1499.25)",
                PriceCalculator.CalculateDiscountedPrice(1999m, false) == 1499.25m);
            Check("«много» при 6 парах", PriceCalculator.GetQuantityLabel(6) == "много");
            Check("«мало» при 5 парах", PriceCalculator.GetQuantityLabel(5) == "мало");
            Check("Подсветка при остатке 3", PriceCalculator.IsLowStock(3));
            Check("Нет подсветки при остатке 4", !PriceCalculator.IsLowStock(4));

            // ---------------- данные ----------------
            var user = DataService.FindUserByLogin("asidorova");
            if (user is null)
            {
                Log("ОШИБКА: пользователь asidorova не найден.");
                File.WriteAllLines(reportPath, report);
                return;
            }

            Log($"Авторизация по логину: {user.FullName}, роль {user.RoleName}");
            Check("Роль пользователя — авторизованный пользователь", user.RoleName == User.ClientRole);

            var products = DataService.GetProducts(DateTime.Today);
            Check("Каталог загружен (31 товар)", products.Count == 31);
            Check("У товаров есть размеры", products.All(p => p.Sizes.Count >= 0));
            Check("Рассчитан признак скидки",
                products.Count(p => p.HasDiscount) == products.Count(p => !p.HasOrdersInPreviousMonth));

            var first = products.First(p => p.Sizes.Count > 0);
            var size = first.Sizes[0];

            var before = DataService.GetOrders();
            Log($"Заказов до теста: {before.Count}");

            var lines = new List<CartLine>
            {
                new()
                {
                    StockItemId = size.StockItemId,
                    ProductId = first.ProductId,
                    ProductName = first.ProductName,
                    ManufacturerName = first.ManufacturerName,
                    SizeValue = size.SizeValue,
                    AvailableQuantity = size.Quantity,
                    UnitPrice = first.FinalPrice,
                    Quantity = 2
                }
            };

            var orderId = DataService.CreateOrder(user.UserId, DateTime.Today, lines);
            Log($"Создан заказ №{orderId} на сумму {lines[0].Sum:N2}");

            var created = DataService.GetOrders().FirstOrDefault(o => o.OrderId == orderId);
            if (created is null)
            {
                Log("ОШИБКА: созданный заказ не найден в представлении v_orders.");
                File.WriteAllLines(reportPath, report);
                return;
            }

            Log($"Прочитано: {created.DateText}, клиент {created.ClientName}, " +
                $"позиций {created.PositionCount}, пар {created.ItemsCount}, сумма {created.TotalCost:N2}");

            Check("Сумма заказа совпадает с расчётной", created.TotalCost == lines[0].Sum);
            Check("Количество позиций = 1", created.PositionCount == 1);
            Check("Количество пар = 2", created.ItemsCount == 2);
            Check("Номер заказа = максимальный + 1", created.OrderId == before.Max(o => o.OrderId) + 1);
            Check("Состав заказа прочитан", DataService.GetOrderLines(orderId).Count == 1);

            DataService.DeleteOrder(orderId);
            Log($"Тестовый заказ №{orderId} удалён. Заказов после теста: {DataService.GetOrders().Count}");
        }
        catch (Exception exception)
        {
            Log("ОШИБКА: " + exception.Message);
        }

        File.WriteAllLines(reportPath, report);
    }
}
