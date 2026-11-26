namespace ChudoObuv.Logic;

/// <summary>Алгоритмы расчёта показателей товара (по методическому пособию).</summary>
public static class PriceCalculator
{
    public const decimal DiscountPercent = 25m;
    public const int ManyQuantityThreshold = 5;
    public const int LowStockThreshold = 3;

    /// <summary>Начало текущего календарного месяца относительно даты расчёта.</summary>
    public static DateTime GetCurrentMonthStart(DateTime calculationDate) =>
        new(calculationDate.Year, calculationDate.Month, 1);

    /// <summary>Начало предыдущего календарного месяца.</summary>
    public static DateTime GetPreviousMonthStart(DateTime calculationDate) =>
        GetCurrentMonthStart(calculationDate).AddMonths(-1);

    /// <summary>
    /// Цена со скидкой 25 %, если в предыдущем календарном месяце заказов товара не было.
    /// </summary>
    public static decimal CalculateDiscountedPrice(decimal price, bool hasOrdersInPreviousMonth)
    {
        if (hasOrdersInPreviousMonth)
        {
            return price;
        }

        return Math.Round(price * (100m - DiscountPercent) / 100m, 2);
    }

    /// <summary>«много», если пар больше пяти, иначе «мало».</summary>
    public static string GetQuantityLabel(int totalQuantity) =>
        totalQuantity > ManyQuantityThreshold ? "много" : "мало";

    /// <summary>Остаток не больше трёх пар — подсветка карточки.</summary>
    public static bool IsLowStock(int totalQuantity) => totalQuantity <= LowStockThreshold;
}
