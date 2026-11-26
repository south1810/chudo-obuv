using ChudoObuv.Models;

namespace ChudoObuv.Data;

/// <summary>Текущий пользователь и формируемый заказ.</summary>
public static class Session
{
    public static User? CurrentUser { get; private set; }

    public static List<CartLine> Cart { get; } = new();

    public static bool IsAuthorized => CurrentUser is not null;

    public static bool IsManager => CurrentUser?.IsManager == true;

    public static bool IsAdministrator => CurrentUser?.IsAdministrator == true;

    public static void SignIn(User user)
    {
        CurrentUser = user;
        Cart.Clear();
    }

    public static void ContinueAsGuest()
    {
        CurrentUser = null;
        Cart.Clear();
    }

    public static void Logout()
    {
        CurrentUser = null;
        Cart.Clear();
    }

    public static decimal CartTotal => Cart.Sum(line => line.Sum);

    public static int CartPositions => Cart.Count;

    public static int CartItems => Cart.Sum(line => line.Quantity);
}
