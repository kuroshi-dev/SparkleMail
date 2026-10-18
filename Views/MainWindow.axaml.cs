using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using SparkleMail.Models;
using SparkleMail.Services;

namespace SparkleMail;

public partial class MainWindow : Window
{
    private readonly MailKitEmailService _mail = new();
    private AccountSettings _settings = SettingsStorage.Load();
    private bool _showSent;   // false = Вхідні, true = Надіслані

    private bool HasAccount =>
        !string.IsNullOrWhiteSpace(_settings.Email) && !string.IsNullOrWhiteSpace(_settings.Password);

    public MainWindow()
    {
        InitializeComponent();

        FillSettingsFields();
        UpdateAccountState();

        BtnSave.Click += OnSave;
        BtnTest.Click += OnTest;
        BtnRefresh.Click += async (_, _) => await RefreshAsync();
        BtnSend.Click += OnSend;
        MessageList.SelectionChanged += OnMessageSelected;
        FolderList.SelectionChanged += OnFolderChanged;
        TxtTo.TextChanged += (_, _) => AddrCount.Text =
            $"Кілька адрес, кожна з нового рядка - це розсилка · Адрес: {ParseRecipients().Count}";

        // нативний браузер ховаємо, коли відкрита форма листа або налаштування
        BtnCompose.IsCheckedChanged += (_, _) => UpdateWebVisibility();
        BtnSettings.IsCheckedChanged += (_, _) => UpdateWebVisibility();

        Opened += async (_, _) => await RefreshAsync();
    }

    private void UpdateWebVisibility() =>
        ReaderWeb.IsVisible = BtnCompose.IsChecked != true && BtnSettings.IsChecked != true;

    // ---------- Налаштування ----------

    private void FillSettingsFields()
    {
        TxtLogin.Text = _settings.Email;
        TxtPassword.Text = _settings.Password;
        TxtSmtpServer.Text = _settings.SmtpServer;
        TxtSmtpPort.Text = _settings.SmtpPort.ToString();
        TxtImapServer.Text = _settings.ImapServer;
        TxtImapPort.Text = _settings.ImapPort.ToString();
    }

    private AccountSettings ReadSettingsFields() => new()
    {
        Email = TxtLogin.Text?.Trim() ?? "",
        Password = TxtPassword.Text ?? "",
        SmtpServer = TxtSmtpServer.Text?.Trim() ?? "",
        SmtpPort = int.TryParse(TxtSmtpPort.Text, out var sp) ? sp : 465,
        ImapServer = TxtImapServer.Text?.Trim() ?? "",
        ImapPort = int.TryParse(TxtImapPort.Text, out var ip) ? ip : 993
    };

    private void UpdateAccountState()
    {
        NoAccountBanner.IsVisible = !HasAccount;
        TxtAccount.Text = HasAccount ? _settings.Email : "—";
    }

    private async void OnSave(object? s, RoutedEventArgs e)
    {
        _settings = ReadSettingsFields();
        SettingsStorage.Save(_settings);
        UpdateAccountState();
        await RefreshAsync();
    }

    private async void OnTest(object? s, RoutedEventArgs e)
    {
        SetStatus("Перевірка з'єднання…");
        bool ok = await _mail.TestConnectionAsync(ReadSettingsFields());
        SetStatus(ok ? "● З'єднання успішне" : "● Помилка з'єднання");
    }

    // ---------- Папки ----------

    private async void OnFolderChanged(object? s, SelectionChangedEventArgs e)
    {
        _showSent = FolderList.SelectedIndex == 1;
        ListTitle.Text = _showSent ? "Надіслані" : "Вхідні";
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (!HasAccount) return;

        SetStatus("Оновлення…");
        try
        {
            var items = await _mail.FetchFolderAsync(_settings, _showSent);
            MessageList.ItemsSource = items;

            if (!_showSent)
                BadgeCount.Text = items.Count(m => !m.IsSeen).ToString();

            string folderName = _showSent ? "Надіслані" : "Вхідні";
            SetStatus($"● IMAP · {_settings.ImapServer} · {folderName} · оновлено {DateTime.Now:HH:mm}");
        }
        catch (Exception ex)
        {
            SetStatus("Помилка: " + ex.Message);
        }
    }

    // ---------- Читання листа ----------

    private async void OnMessageSelected(object? s, SelectionChangedEventArgs e)
    {
        if (MessageList.SelectedItem is not EmailMessageModel m) return;

        ReaderSubject.Text = m.Subject;
        ReaderAvatar.Text = m.Initial;
        ReaderSenderName.Text = m.Sender;
        ReaderSenderEmail.Text = _showSent ? "Кому: " + m.SenderEmail : m.SenderEmail;
        ReaderDate.Text = m.Date.ToString("d MMMM, HH:mm");

        ReaderWeb.NavigateToString("<body style='font-family:sans-serif;color:gray'>Завантаження…</body>");

        try
        {
            string html = await _mail.GetMessageHtmlAsync(_settings, m.UniqueId, _showSent);
            ReaderWeb.NavigateToString(html);

            if (!_showSent && !m.IsSeen)
            {
                m.IsSeen = true;
                if (int.TryParse(BadgeCount.Text, out var n) && n > 0)
                    BadgeCount.Text = (n - 1).ToString();
            }
        }
        catch (Exception ex)
        {
            ReaderWeb.NavigateToString("<body>" + WebUtility.HtmlEncode(ex.Message) + "</body>");
        }
    }

    // ---------- Відправка / розсилка ----------

    private List<string> ParseRecipients() =>
        (TxtTo.Text ?? "")
            .Split(new[] { '\n', '\r', ',', ';', ' ' },
                   StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

    private async void OnSend(object? s, RoutedEventArgs e)
    {
        var recipients = ParseRecipients();
        if (recipients.Count == 0) return;

        BtnSend.IsEnabled = false;
        int done = 0, failed = 0;

        foreach (var r in recipients)
        {
            try
            {
                await _mail.SendEmailAsync(_settings, r, TxtSubject.Text ?? "", TxtBody.Text ?? "");
                done++;
            }
            catch
            {
                failed++;
            }
            StatusRight.Text = $"Розсилка: {done + failed} / {recipients.Count}";
        }

        SetStatus(failed == 0 ? "● Надіслано" : $"● Надіслано: {done}, помилок: {failed}");
        BtnSend.IsEnabled = true;

        if (failed == 0)
        {
            TxtTo.Text = TxtSubject.Text = TxtBody.Text = "";
            BtnCompose.IsChecked = false;
        }
        
        if (_showSent) await RefreshAsync();
    }

    private void SetStatus(string text) => StatusLeft.Text = text;
}