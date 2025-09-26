using Avalonia;
using Avalonia.Controls;
using Microsoft.Data.Sqlite;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using Tmds.DBus.Protocol;
//using Avalonia.Markup.Xaml;

namespace Qc_App;

public partial class HistoryWindow : Window
{
    private string _dbPath = Path.Combine(AppContext.BaseDirectory, "qc.db");
        public HistoryWindow()
        {
            InitializeComponent();
            LoadHistory();

            SearhButon.Click += (_, __) => LoadHistory(SearchInput.Text.Trim());
            ExportButton.Click += (_, __) => ExportToCsv();

        }


        private void LoadHistory(string barcodeFilter = "")
        {
            if (!string.IsNullOrWhiteSpace(barcodeFilter))
                query += " WHERE Barcode LIKE @barcode";

            var cmd = new SqliteCommand(query, connection);
            if (!string.IsNullOrWhiteSpace(barcodeFilter))
                cmd.Parameters.AddWithValue("@barcode", $% "{barcode}%");


            var adapter = new SqliteDataAdapter(cmd);
            var dt = new DataTable();
            adapter.Fill(dt);

            HistoryGrid.Items = dt.DefaultView;
        }


        private void ExportToCsv()
        {
            string exportPath = Path.Combine(AppContext.BaseDirectory, $"qc_export_{DateTime.Now:yyyyMMddHHmmss}.csv");
            using var connection = new SqliteConnection($"Data Source={_dbPath}");
            connection.Open();
            
            var cmd = connection .CreateCommand();
            cmd.CommandText = "SELECT Id, Barcode, Result, FailReason, Timestamp FROM QCResults ORDER by Id DESC";
    
            using var reader = cmd.ExecuteReader();
            using var writer = new StreamWriter(exportPath);

            //Header
             writer.WriteLine("Id, Barcode, Result, FailReason, Timestamp");
            while (reader.Read())
            {
                var line = $"{reader["id"]}, {reader["Barcode"]}, {reader["Result"]}, {reader["FailReason"]}, {reader["Timestamp"]}";
                writer.WriteLine(line);
            }


            writer.Close();
        }
    
}