using System.Windows;
using System.Windows.Controls;
using ChudoObuv.Data;
using ChudoObuv.Models;

namespace ChudoObuv.Pages;

public partial class CatalogPage : Page
{
    private List<Product> _products = new();
    private List<Product> _visible = new();

    public CatalogPage()
    {
        InitializeComponent();
        Loaded += Page_Loaded;
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        FilterPanel.Visibility = Session.IsAuthorized ? Visibility.Visible : Visibility.Collapsed;
        OrdersButton.Visibility = Session.IsManager ? Visibility.Visible : Visibility.Collapsed;

        try
        {
            CategoryComboBox.Items.Add("Все категории");
            foreach (var category in DataService.GetCategories())
            {
                CategoryComboBox.Items.Add(category);
            }

            CategoryComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0;

            _products = DataService.GetProducts(DateTime.Today);
            ApplyFilter();
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this),
                "Не удалось загрузить каталог.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        UpdateCartState();
    }

    private void ApplyFilter()
    {
        IEnumerable<Product> items = _products;

        if (Session.IsAuthorized)
        {
            var query = SearchTextBox.Text.Trim().ToLowerInvariant();
            if (query.Length > 0)
            {
                items = items.Where(p =>
                    p.ProductName.ToLowerInvariant().Contains(query) ||
                    (p.Description ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    (p.Composition ?? string.Empty).ToLowerInvariant().Contains(query) ||
                    p.ManufacturerName.ToLowerInvariant().Contains(query) ||
                    p.CategoryName.ToLowerInvariant().Contains(query));
            }

            if (CategoryComboBox.SelectedIndex > 0 && CategoryComboBox.SelectedItem is string category)
            {
                items = items.Where(p => p.CategoryName == category);
            }
        }

        var result = items.ToList();

        if (Session.IsAuthorized)
        {
            result = SortComboBox.SelectedIndex switch
            {
                1 => result.OrderBy(p => p.FinalPrice).ThenBy(p => p.ProductId).ToList(),
                2 => result.OrderByDescending(p => p.FinalPrice).ThenBy(p => p.ProductId).ToList(),
                _ => result.OrderBy(p => p.ProductId).ToList()
            };
        }
        else
        {
            result = result.OrderBy(p => p.ProductId).ToList();
        }

        _visible = result;
        ProductsListView.ItemsSource = null;
        ProductsListView.ItemsSource = _visible;
        CounterText.Text = $"Показано {_visible.Count} из {_products.Count}";
        SizeComboBox.Items.Clear();
        AddButton.IsEnabled = false;
    }

    private void Filter_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ApplyFilter();
    }

    private void ProductsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SizeComboBox.Items.Clear();
        AddButton.IsEnabled = false;

        if (ProductsListView.SelectedItem is not Product product)
        {
            return;
        }

        foreach (var size in product.Sizes)
        {
            SizeComboBox.Items.Add(size);
        }

        if (SizeComboBox.Items.Count > 0)
        {
            SizeComboBox.SelectedIndex = 0;
            AddButton.IsEnabled = Session.IsAuthorized;
        }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsListView.SelectedItem is not Product product ||
            SizeComboBox.SelectedItem is not SizeStock size)
        {
            return;
        }

        var existing = Session.Cart.FirstOrDefault(l => l.StockItemId == size.StockItemId);
        var already = existing?.Quantity ?? 0;
        if (already + 1 > size.Quantity)
        {
            MessageBox.Show(Window.GetWindow(this),
                $"Размер {size.SizeValue:0.#}: доступно {size.Quantity} пар, в заказе уже {already}.",
                "Недостаточно товара", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (existing is not null)
        {
            existing.Quantity += 1;
        }
        else
        {
            Session.Cart.Add(new CartLine
            {
                StockItemId = size.StockItemId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ManufacturerName = product.ManufacturerName,
                SizeValue = size.SizeValue,
                AvailableQuantity = size.Quantity,
                UnitPrice = product.FinalPrice,
                Quantity = 1
            });
        }

        UpdateCartState();
    }

    private void UpdateCartState()
    {
        CartButton.Content = $"Корзина ({Session.CartPositions})";
        OrderClientText.Text = Session.CurrentUser is null
            ? "Гостевой режим: доступен только просмотр каталога."
            : $"Заказ оформляет: {Session.CurrentUser.FullName} ({Session.CurrentUser.RoleName}). " +
              $"В корзине {Session.CartPositions} поз. на {Session.CartTotal:N2} ₽";
    }

    private void Cart_Click(object sender, RoutedEventArgs e)
    {
        if (Session.Cart.Count == 0)
        {
            MessageBox.Show(Window.GetWindow(this), "Корзина пуста. Добавьте товар из каталога.",
                "Корзина", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        (Window.GetWindow(this) as MainWindow)?.Navigate(new CartPage(null));
    }

    private void Orders_Click(object sender, RoutedEventArgs e)
    {
        (Window.GetWindow(this) as MainWindow)?.Navigate(new OrdersPage());
    }
}
