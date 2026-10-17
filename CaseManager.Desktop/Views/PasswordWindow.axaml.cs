using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CaseManager.Core.Crypto;
using CaseManager.Core.Storage;

namespace CaseManager.Desktop.Views;

public partial class PasswordWindow : Window
{
    public CaseRepository? Repository { get; private set; }

    public PasswordWindow()
    {
        InitializeComponent();
        LoginButton.Click += OnLoginClick;
        PasswordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) OnLoginClick(null, null!);
        };
        Opened += (_, _) => PasswordBox.Focus();
    }

    private void OnLoginClick(object? sender, RoutedEventArgs e)
    {
        var pwd = PasswordBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(pwd))
        {
            ShowError("Введите пароль.");
            return;
        }

        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "CaseManager");

        var probe = new CaseRepository(root, new AesGcmEncryptor(), pwd);
        try
        {
            foreach (var _ in probe.LoadAll()) break;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            ShowError("Неверный пароль или файлы повреждены.");
            return;
        }
        catch (Exception ex)
        {
            ShowError("Ошибка: " + ex.Message);
            return;
        }

        Repository = probe;
        Close(true);
    }

    private void ShowError(string msg)
    {
        ErrorText.Text = msg;
        ErrorText.IsVisible = true;
    }
}