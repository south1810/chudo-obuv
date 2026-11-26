using MySqlConnector;

namespace ChudoObuv.Data;

/// <summary>Фабрика соединений с базой данных магазина.</summary>
public static class Database
{
    private static readonly Lazy<string> LazyConnectionString = new(BuildConnectionString);

    private static string ConnectionString => LazyConnectionString.Value;

    private static string BuildConnectionString()
    {
        var settings = ConnectionSettings.Load();
        return new MySqlConnectionStringBuilder
        {
            Server = settings.Server,
            Port = settings.Port,
            Database = settings.Database,
            UserID = settings.User,
            Password = settings.Password,
            CharacterSet = "utf8mb4",
            SslMode = MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
            ConnectionTimeout = 10,
            DefaultCommandTimeout = 30
        }.ConnectionString;
    }

    public static MySqlConnection OpenConnection()
    {
        var connection = new MySqlConnection(ConnectionString);
        connection.Open();
        return connection;
    }
}
