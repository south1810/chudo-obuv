using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ChudoObuv.Pages;

namespace ChudoObuv;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadLogo();
        MainFrame.Navigate(new LoginPage());
        MainFrame.Navigated += (_, _) => UpdateUserInfo();
        UpdateUserInfo();
    }

    private void LoadLogo()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Resources", "logo.png");
        if (File.Exists(path))
        {
            LogoImage.Source = new BitmapImage(new Uri(path));
        }
    }

    /// <summary>Обновляет подпись пользователя и видимость кнопок в шапке.</summary>
    public void UpdateUserInfo()
    {
        var user = Data.Session.CurrentUser;
        UserInfoText.Text = user is null
            ? "Гость (неавторизованный пользователь)"
            : $"{user.FullName} · {user.RoleName}";
        LogoutButton.Content = user is null ? "Войти" : "Выйти";
        BackButton.Visibility = MainFrame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Navigate(Page page)
    {
        MainFrame.Navigate(page);
        UpdateUserInfo();
    }

    /// <summary>Заменяет страницу без записи в историю переходов.</summary>
    public void NavigateRoot(Page page)
    {
        MainFrame.Navigate(page);
        MainFrame.NavigationService?.RemoveBackEntry();
        while (MainFrame.CanGoBack)
        {
            MainFrame.RemoveBackEntry();
        }

        UpdateUserInfo();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (MainFrame.CanGoBack)
        {
            MainFrame.GoBack();
        }

        UpdateUserInfo();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        if (Data.Session.CurrentUser is not null &&
            Data.Session.Cart.Count > 0 &&
            MessageBox.Show(this, "Неподтверждённый заказ будет отменён. Выйти из системы?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        Data.Session.Logout();
        NavigateRoot(new LoginPage());
    }
}
