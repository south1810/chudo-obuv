using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ChudoObuv.Data;
using ChudoObuv.Models;

namespace ChudoObuv.Pages;

public partial class CartPage : Page
{
    private readonly int? _orderId;
    private readonly ObservableCollection<CartLine> _lines = new();
    private List<User> _clients = new();

    public CartPage(int? orderId)
    {
        _orderId = orderId;
        InitializeComponent();
        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        HeaderText.Text = _orderId is null
            ? "Оформление заказа"
            : $"Изменение заказа №{_orderId}";

        try
        {
            _clients = BuildClientList();
            if (_orderId is null)
            {
                foreach (var line in Session.Cart)
                {
                    _lines.Add(line);
                }
            }
            else
            {
                foreach (var line in LoadOrderLines(_orderId.Value))
                {
                    _lines.Add(line);
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), "Не удалось загрузить заказ.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        FillClients();
        LinesListView.ItemsSource = _lines;
        foreach (var line in _lines)
        {
            line.PropertyChanged += Line_PropertyChanged;
        }

        UpdateTotals();
    }

    /// <summary>Клиенты для выбора: менеджер и администратор выбирают любого, остальные — себя.</summary>
    private List<User> BuildClientList()
    {
        var clients = DataService.GetClients();
        var current = Session.CurrentUser;
        if (current is null)
        {
            return clients;
        }

        if (!Session.IsManager)
        {
            return new List<User> { current };
        }

        if (clients.All(c => c.UserId != current.UserId))
        {
            clients.Insert(0, current);
        }

        return clients;
    }

    private List<CartLine> LoadOrderLines(int orderId)
    {
        return DataService.GetOrderLines(orderId)
            .Select(line => new CartLine
            {
                StockItemId = line.StockItemId,
                ProductName = line.ProductName,
                ManufacturerName = line.ManufacturerName,
                SizeValue = line.SizeValue,
                AvailableQuantity = 0,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity
            })
            .ToList();
    }

    private void FillClients()
    {
        ClientComboBox.Items.Clear();
        foreach (var client in _clients)
        {
            ClientComboBox.Items.Add($"{client.FullName} ({client.RoleName})");
        }

        if (_orderId is not null)
        {
            var order = DataService.GetOrders().FirstOrDefault(o => o.OrderId == _orderId.Value);
            var index = order is null ? -1 : _clients.FindIndex(c => c.UserId == order.UserId);
            ClientComboBox.SelectedIndex = index >= 0 ? index : 0;
        }
        else if (Session.CurrentUser is not null)
        {
            var index = _clients.FindIndex(c => c.UserId == Session.CurrentUser.UserId);
            ClientComboBox.SelectedIndex = index >= 0 ? index : 0;
        }

        var canChoose = Session.IsManager;
        ClientComboBox.IsEnabled = canChoose;
        ClientLockedText.Text = canChoose
            ? "Клиента можно изменить (менеджер или администратор)."
            : "Клиент — текущий авторизованный пользователь.";
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateTotals();

    private void UpdateTotals()
    {
        TotalText.Text = $"Позиций: {_lines.Count} · пар: {_lines.Sum(l => l.Quantity)} · " +
                         $"итого: {_lines.Sum(l => l.Sum):N2} ₽";
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: CartLine line })
        {
            line.PropertyChanged -= Line_PropertyChanged;
            _lines.Remove(line);
            if (_orderId is null)
            {
                Session.Cart.Remove(line);
            }

            UpdateTotals();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this) as MainWindow;
        if (window is null)
        {
            return;
        }

        if (window.MainFrame.CanGoBack)
        {
            window.MainFrame.GoBack();
        }
        else
        {
            window.NavigateRoot(new CatalogPage());
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_lines.Count == 0)
        {
            MessageBox.Show(Window.GetWindow(this), "В заказе должна быть хотя бы одна позиция.",
                "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (ClientComboBox.SelectedIndex < 0 || ClientComboBox.SelectedIndex >= _clients.Count)
        {
            MessageBox.Show(Window.GetWindow(this), "Выберите клиента.",
                "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var client = _clients[ClientComboBox.SelectedIndex];

        try
        {
            if (_orderId is null)
            {
                var orderId = DataService.CreateOrder(client.UserId, DateTime.Today, _lines.ToList());
                Session.Cart.Clear();
                MessageBox.Show(Window.GetWindow(this), $"Заказ №{orderId} оформлен.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                DataService.UpdateOrder(_orderId.Value, client.UserId, DateTime.Today, _lines.ToList());
                MessageBox.Show(Window.GetWindow(this), $"Заказ №{_orderId} сохранён.",
                    "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            var window = Window.GetWindow(this) as MainWindow;
            if (Session.IsManager)
            {
                window?.NavigateRoot(new OrdersPage());
            }
            else
            {
                window?.NavigateRoot(new CatalogPage());
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), "Не удалось сохранить заказ.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
