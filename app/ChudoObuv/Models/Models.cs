using System.IO;
using System.Windows.Media.Imaging;
using ChudoObuv.Logic;

namespace ChudoObuv.Models;

/// <summary>Пользователь системы (вход по логину, без пароля).</summary>
public sealed class User
{
    public const string ClientRole = "Авторизованный пользователь";
    public const string ManagerRole = "Менеджер";
    public const string AdminRole = "Администратор";

    public int UserId { get; init; }
    public string LastName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string? Patronymic { get; init; }
    public string Login { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;

    public string FullName =>
        string.Join(' ', new[] { LastName, FirstName, Patronymic }
            .Where(part => !string.IsNullOrWhiteSpace(part)));

    public bool IsManager => RoleName is ManagerRole or AdminRole;

    public bool IsAdministrator => RoleName == AdminRole;

    public override string ToString() => $"{FullName} ({RoleName})";
}

/// <summary>Доступный размер модели с количеством на складе.</summary>
public sealed class SizeStock
{
    public int StockItemId { get; init; }
    public decimal SizeValue { get; init; }
    public int Quantity { get; init; }

    public string Display => $"{SizeValue:0.#} ({Quantity} шт.)";
}

/// <summary>Товар в каталоге с расчётными показателями.</summary>
public sealed class Product
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string SubcategoryName { get; init; } = string.Empty;
    public string ManufacturerName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Composition { get; init; }
    public decimal Price { get; init; }
    public string? ImageFile { get; init; }
    public int TotalQuantity { get; init; }
    public bool HasOrdersInPreviousMonth { get; init; }
    public List<SizeStock> Sizes { get; init; } = new();

    public string Title => $"{ManufacturerName} | {ProductName}";

    public decimal FinalPrice =>
        PriceCalculator.CalculateDiscountedPrice(Price, HasOrdersInPreviousMonth);

    public bool HasDiscount => Math.Abs(FinalPrice - Price) > 0.001m;

    public string OldPriceText => HasDiscount ? $"{Price:N2} ₽" : string.Empty;

    public string PriceText => $"{FinalPrice:N2} ₽";

    public bool IsLowStock => PriceCalculator.IsLowStock(TotalQuantity);

    public string QuantityLabel => PriceCalculator.GetQuantityLabel(TotalQuantity);

    public string QuantityText => $"{TotalQuantity} пар ({QuantityLabel})";

    /// <summary>Изображение товара или картинка-заглушка из ресурсов.</summary>
    public BitmapImage? ImagePath
    {
        get
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "Images");
            var path = string.IsNullOrWhiteSpace(ImageFile)
                ? Path.Combine(folder, "picture.png")
                : Path.Combine(folder, ImageFile);
            if (!File.Exists(path))
            {
                path = Path.Combine(folder, "picture.png");
            }

            return File.Exists(path) ? new BitmapImage(new Uri(path)) : null;
        }
    }
}

/// <summary>Строка формируемого заказа.</summary>
public sealed class CartLine : System.ComponentModel.INotifyPropertyChanged
{
    private int _quantity = 1;

    public int StockItemId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ManufacturerName { get; init; } = string.Empty;
    public decimal SizeValue { get; init; }
    public int AvailableQuantity { get; init; }
    public decimal UnitPrice { get; init; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 1)
            {
                value = 1;
            }

            if (AvailableQuantity > 0 && value > AvailableQuantity)
            {
                value = AvailableQuantity;
            }

            if (_quantity == value)
            {
                return;
            }

            _quantity = value;
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(Sum));
            OnPropertyChanged(nameof(SumText));
        }
    }

    public decimal Sum => UnitPrice * Quantity;

    public string SizeText => SizeValue.ToString("0.#");

    public string Title => $"{ManufacturerName} | {ProductName}";

    public string UnitPriceText => $"{UnitPrice:N2} ₽";

    public string SumText => $"{Sum:N2} ₽";

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
}

/// <summary>Заказ в списке.</summary>
public sealed class OrderRow
{
    public int OrderId { get; init; }
    public DateTime OrderDate { get; init; }
    public int UserId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public int PositionCount { get; init; }
    public int ItemsCount { get; init; }
    public decimal TotalCost { get; init; }

    public string DateText => OrderDate.ToString("dd.MM.yyyy");
    public string TotalText => $"{TotalCost:N2} ₽";
}

/// <summary>Строка состава заказа.</summary>
public sealed class OrderLine
{
    public int OrderItemId { get; init; }
    public int StockItemId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ManufacturerName { get; init; } = string.Empty;
    public decimal SizeValue { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    public decimal Sum => UnitPrice * Quantity;
    public string Title => $"{ManufacturerName} | {ProductName}";
    public string SizeText => SizeValue.ToString("0.#");
    public string SumText => $"{Sum:N2} ₽";
}
