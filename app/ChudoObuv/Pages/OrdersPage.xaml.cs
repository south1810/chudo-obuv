using System.Windows;
using System.Windows.Controls;
using ChudoObuv.Data;
using ChudoObuv.Models;

namespace ChudoObuv.Pages;

public partial class OrdersPage : Page
{
    private List<OrderRow> _orders = new();

    public OrdersPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            DeleteButton.Visibility = Session.IsManager ? Visibility.Visible : Visibility.Collapsed;
            EditButton.Visibility = Session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
            LoadOrders();
        };
    }

    private void LoadOrders()
    {
        try
        {
            _orders = DataService.GetOrders();
            OrdersListView.ItemsSource = null;
            OrdersListView.ItemsSource = _orders;

            if (_orders.Count > 0)
            {
                OrdersListView.SelectedIndex = 0;
            }
            else
            {
                CompositionListView.ItemsSource = null;
                CompositionTitle.Text = "Состав заказа";
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), "Не удалось загрузить заказы.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Orders_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersListView.SelectedItem is not OrderRow order)
        {
            return;
        }

        try
        {
            var lines = DataService.GetOrderLines(order.OrderId);
            CompositionListView.ItemsSource = null;
            CompositionListView.ItemsSource = lines;
            CompositionTitle.Text =
                $"Состав заказа №{order.OrderId} от {order.DateText} · {order.ClientName} · " +
                $"итого {order.TotalText}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), "Не удалось загрузить состав заказа.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadOrders();

    private void Catalog_Click(object sender, RoutedEventArgs e) =>
        (Window.GetWindow(this) as MainWindow)?.NavigateRoot(new CatalogPage());

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersListView.SelectedItem is not OrderRow order)
        {
            MessageBox.Show(Window.GetWindow(this), "Выберите заказ в списке.",
                "Заказ не выбран", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (MessageBox.Show(Window.GetWindow(this),
                $"Удалить заказ №{order.OrderId} от {order.DateText}?", "Подтверждение удаления",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            DataService.DeleteOrder(order.OrderId);
            LoadOrders();
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), "Не удалось удалить заказ.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersListView.SelectedItem is not OrderRow order)
        {
            MessageBox.Show(Window.GetWindow(this), "Выберите заказ в списке.",
                "Заказ не выбран", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        (Window.GetWindow(this) as MainWindow)?.Navigate(new CartPage(order.OrderId));
    }
}
