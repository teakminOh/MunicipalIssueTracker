using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MunicipalIssueTracker.WinFormsImporter;

public class MainForm : Form
{
    private readonly TextBox _txtApiUrl;
    private readonly TextBox _txtFilePath;
    private readonly Button _btnBrowse;
    private readonly Button _btnImport;
    private readonly DataGridView _gridPreview;
    private readonly Label _lblStatus;
    private readonly ProgressBar _progressBar;

    private List<CsvIssueRow>? _parsedRows;

    public MainForm()
    {
        Text = "Municipal Issue Tracker - CSV Importer";
        Width = 900;
        Height = 600;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // API URL
        var lblApi = new Label { Text = "API URL:", Left = 12, Top = 15, Width = 60 };
        _txtApiUrl = new TextBox { Left = 75, Top = 12, Width = 400, Text = "http://localhost:5000" };

        // File path
        var lblFile = new Label { Text = "CSV File:", Left = 12, Top = 45, Width = 60 };
        _txtFilePath = new TextBox { Left = 75, Top = 42, Width = 400, ReadOnly = true };
        _btnBrowse = new Button { Text = "Browse...", Left = 480, Top = 40, Width = 80 };
        _btnBrowse.Click += BtnBrowse_Click;

        // Import button
        _btnImport = new Button { Text = "Import Issues", Left = 570, Top = 40, Width = 120, Enabled = false };
        _btnImport.Click += async (s, e) => await BtnImport_Click();

        // Preview grid
        _gridPreview = new DataGridView
        {
            Left = 12, Top = 80, Width = 860, Height = 400,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect
        };

        // Status
        _lblStatus = new Label { Text = "Select a CSV file to preview and import.", Left = 12, Top = 490, Width = 600, Height = 20 };
        _progressBar = new ProgressBar { Left = 12, Top = 515, Width = 860, Height = 20 };

        Controls.AddRange(new Control[]
        {
            lblApi, _txtApiUrl, lblFile, _txtFilePath, _btnBrowse, _btnImport,
            _gridPreview, _lblStatus, _progressBar
        });
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
            Title = "Select CSV file with municipal issues"
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        _txtFilePath.Text = ofd.FileName;
        LoadCsvPreview(ofd.FileName);
    }

    private void LoadCsvPreview(string filePath)
    {
        try
        {
            _parsedRows = ParseCsv(filePath);
            _gridPreview.DataSource = _parsedRows;
            _btnImport.Enabled = _parsedRows.Count > 0;
            _lblStatus.Text = $"Loaded {_parsedRows.Count} issue(s) from CSV.";
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error reading CSV: {ex.Message}";
            _btnImport.Enabled = false;
        }
    }

    private static List<CsvIssueRow> ParseCsv(string filePath)
    {
        var rows = new List<CsvIssueRow>();
        var lines = File.ReadAllLines(filePath);

        if (lines.Length < 2) return rows; // Header + at least one data row

        // Expected CSV columns: Title,Description,Category,District,Lat,Lng,Address,Priority
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = SplitCsvLine(line);
            if (parts.Length < 6) continue;

            rows.Add(new CsvIssueRow
            {
                Title = parts[0],
                Description = parts.Length > 1 ? parts[1] : "",
                Category = parts.Length > 2 ? parts[2] : "Other",
                District = parts.Length > 3 ? parts[3] : "Centrum",
                Lat = double.TryParse(parts[4], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lat) ? lat : 59.33,
                Lng = double.TryParse(parts[5], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var lng) ? lng : 18.07,
                Address = parts.Length > 6 ? parts[6] : "",
                Priority = parts.Length > 7 ? parts[7] : "Medium"
            });
        }

        return rows;
    }

    /// <summary>
    /// Simple CSV line splitter that handles quoted fields.
    /// </summary>
    private static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var current = "";
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += ch;
            }
        }
        result.Add(current.Trim());
        return result.ToArray();
    }

    private async Task BtnImport_Click()
    {
        if (_parsedRows == null || _parsedRows.Count == 0) return;

        var apiUrl = _txtApiUrl.Text.TrimEnd('/');
        _btnImport.Enabled = false;
        _progressBar.Value = 0;
        _progressBar.Maximum = _parsedRows.Count;

        try
        {
            _lblStatus.Text = "Importing issues...";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var payload = _parsedRows.Select(r => new
            {
                title = r.Title,
                description = r.Description,
                category = r.Category,
                district = r.District,
                lat = r.Lat,
                lng = r.Lng,
                address = r.Address,
                priority = r.Priority
            }).ToList();

            var response = await client.PostAsJsonAsync($"{apiUrl}/api/issues/import", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ImportResult>();
                _lblStatus.Text = $"Successfully imported {result?.Imported ?? 0} issue(s).";
                _progressBar.Value = _progressBar.Maximum;
                MessageBox.Show(
                    $"Import complete!\n\n{result?.Imported ?? 0} issue(s) imported successfully.",
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _lblStatus.Text = $"Import failed: HTTP {response.StatusCode}";
                MessageBox.Show(
                    $"Import failed.\n\nHTTP {response.StatusCode}\n{error}",
                    "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            _lblStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Import error:\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _btnImport.Enabled = true;
        }
    }

    private record ImportResult(int Imported);
}

public class CsvIssueRow
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string District { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Address { get; set; } = "";
    public string Priority { get; set; } = "Medium";
}
