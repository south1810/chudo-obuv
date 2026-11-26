using System.Windows;
using System.Windows.Controls;
using ChudoObuv.Data;

namespace ChudoObuv.Pages;

public partial class LoginPage : Page
{
    public LoginPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoginTextBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var login = LoginTextBox.Text.Trim();
        if (login.Length == 0)
        {
            ErrorText.Text = "Введите логин.";
            return;
        }

        try
        {
            var user = DataService.FindUserByLogin(login);
            if (user is null)
            {
                ErrorText.Text = "Пользователь с таким логином не найден.";
                return;
            }

            Session.SignIn(user);
            (Window.GetWindow(this) as MainWindow)?.NavigateRoot(new CatalogPage());
        }
        catch (Exception exception)
        {
            ErrorText.Text = "Не удалось подключиться к базе данных.";
            MessageBox.Show(Window.GetWindow(this),
                "Не удалось подключиться к базе данных.\n\n" + exception.Message,
                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Guest_Click(object sender, RoutedEventArgs e)
    {
        Session.ContinueAsGuest();
        (Window.GetWindow(this) as MainWindow)?.NavigateRoot(new CatalogPage());
    }
}
