using System.IO;
using System.Text.Json;

namespace ChudoObuv.Data;

/// <summary>Параметры подключения к базе данных (файл connection.json рядом с exe).</summary>
public sealed class ConnectionSettings
{
    public string Server { get; init; } = "localhost";
    public uint Port { get; init; } = 3306;
    public string Database { get; init; } = "isp-45_dolgih_ka_chudo_obuv";
    public string User { get; init; } = "root";
    public string Password { get; init; } = string.Empty;

    public const string FileName = "connection.json";

    public static ConnectionSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Не найден файл настроек «{FileName}». Скопируйте connection.sample.json " +
                "в connection.json и укажите параметры сервера БД.", path);
        }

        var settings = JsonSerializer.Deserialize<ConnectionSettings>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (settings is null || string.IsNullOrWhiteSpace(settings.Server))
        {
            throw new InvalidOperationException($"Файл «{FileName}» заполнен некорректно.");
        }

        return settings;
    }
}
