using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace Qc_App;

public partial class MainWindow : Window
{
    private string _dbPath = Path.Combine(AppContext.BaseDirectory, "qc.db");

    public MainWindow()
    {
        InitializeComponent();
        EnsureDatabase();

        PassButton.Click += (_, __) => SaveResult("PASS");
        FailButton.Click += (_, __) => SaveResult("FAIL");

        // Show OtherReasonInput only when "Other" is selected
        FailReasonDropdown.SelectionChanged += (_, __) =>
        {
            var selected = (FailReasonDropdown.SelectedItem as ComboBoxItem)?.Content.ToString();
            OtherReasonInput.IsVisible = (selected == "Other");
        };
        OtherReasonInput.IsVisible = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void EnsureDatabase()
    {
        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        string tableCmd = @"
            CREATE TABLE IF NOT EXISTS QCResults (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Barcode TEXT NOT NULL,
                Result TEXT NOT NULL,
                FailReason TEXT,
                Timestamp TEXT NOT NULL
            )";
        using var cmd = new SqliteCommand(tableCmd, connection);
        cmd.ExecuteNonQuery();
    }

    private void SaveResult(string result)
    {
        if (string.IsNullOrWhiteSpace(ScanInput.Text))
        {
            StatusLabel.Text = "Please scan an item first.";
            return;
        }

        string failReason = null;
        if (result == "FAIL")
        {
            var selectedItem = (FailReasonDropdown.SelectedItem as ComboBoxItem)?.Content.ToString();
            failReason = selectedItem == "Other" ? OtherReasonInput.Text : selectedItem;

            if (string.IsNullOrWhiteSpace(failReason))
            {
                StatusLabel.Text = "Please select or enter a fail reason.";
                return;
            }
        }

        using var connection = new SqliteConnection($"Data Source={_dbPath}");
        connection.Open();

        string insertCmd = "INSERT INTO QCResults (Barcode, Result, FailReason, Timestamp) VALUES (@barcode, @result, @failReason, @ts)";
        using var cmd = new SqliteCommand(insertCmd, connection);
        cmd.Parameters.AddWithValue("@barcode", ScanInput.Text.Trim());
        cmd.Parameters.AddWithValue("@result", result);
        cmd.Parameters.AddWithValue("@failReason", (object?)failReason ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ts", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.ExecuteNonQuery();

        StatusLabel.Text = $"Saved {result} for {ScanInput.Text}";

        ScanInput.Text = "";
        ScanInput.Focus();
    }
}
