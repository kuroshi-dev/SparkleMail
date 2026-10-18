using System;
using System.IO;
using System.Text.Json;
using SparkleMail.Models;

namespace SparkleMail.Services;

public static class SettingsStorage
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SparkleMail",
        "settings.json"
    );

    public static AccountSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AccountSettings>(json) ?? new AccountSettings();
            }
        }
        catch
        {
            // Если файл поврежден или не читается, возвращаем дефолтные настройки
        }

        return new AccountSettings();
    }

    public static void Save(AccountSettings settings)
    {
        try
        {
            string? dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Обработка ошибок записи
        }
    }
}