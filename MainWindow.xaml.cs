using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Windows;

namespace Exporter
{
    // Main application window class
    public partial class MainWindow : Window
    {
        private DatabaseService dbService;
        private BackgroundWorker importWorker;
        private CancellationTokenSource cts;

        public MainWindow()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        // Initialize the background worker process
        private void InitializeBackgroundWorker()
        {
            importWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            importWorker.DoWork += ImportWorker_DoWork;
            importWorker.ProgressChanged += ImportWorker_ProgressChanged;
            importWorker.RunWorkerCompleted += ImportWorker_RunWorkerCompleted;
        }

        // Handler for clicking the "Browse" button to select a CSV file
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Select CSV File",
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                txtCsvPath.Text = openFileDialog.FileName;
            }
        }

        // Handler for clicking the "Connect" button to the database
        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtServer.Text)) 
                    throw new ArgumentException("Server is required");

                if (string.IsNullOrWhiteSpace(txtDatabase.Text)) 
                    throw new ArgumentException("Database is required");

                using (var connection = new SqlConnection(GetConnectionString()))
                {
                    connection.Open();
                }

                dbService = new DatabaseService(txtServer.Text, txtDatabase.Text);
                LogMessage("Connected to database successfully");
            }
            catch (Exception ex)
            {
                ShowError($"Connection error: {ex.Message}");
                dbService = null;
            }
        }

        // Generate a database connection string
        private string GetConnectionString()
        {
            return new SqlConnectionStringBuilder
            {
                DataSource = txtServer.Text,
                InitialCatalog = txtDatabase.Text,
                IntegratedSecurity = true,
                ConnectTimeout = 10
            }.ToString();
        }

        // Handler for clicking the "Import" button to load data from CSV
        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dbService == null) 
                    throw new InvalidOperationException("Database connection required");

                if (string.IsNullOrWhiteSpace(txtCsvPath.Text)) 
                    throw new InvalidOperationException("CSV file required");

                if (!File.Exists(txtCsvPath.Text)) 
                    throw new InvalidOperationException("File not found");

                importWorker.RunWorkerAsync(txtCsvPath.Text);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        // Method executed in background worker process to import data
        private void ImportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var filePath = e.Argument as string;
                var parser = new CsvParserService();
                var records = parser.ParseFile(filePath);

                int startId = dbService.GetCurrentMaxId();
                e.Result = startId;

                dbService.ImportData(records, importWorker);
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"CSV processing error: {ex.Message}"));
                e.Cancel = true;
            }

        }

        // Method to update import progress
        private void ImportWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            txtLog.AppendText($"{e.UserState}\n");
        }

        // Method to execute when background worker process completes
        private void ImportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnCancel.IsEnabled = true;

            if (e.Cancelled)
            {
                int startId = (int)e.Result;
                dbService.DeleteRecordsAfterId(startId);
                LogMessage("Import cancelled. All changes rolled back.");
            }
            else if (e.Error != null)
            {
                ShowError($"Import failed: {e.Error.Message}");
            }
            else
            {
                LogMessage("Import completed successfully!");
            }
        }

        // Event handler for the Cancel button click event
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (importWorker.IsBusy)
            {
                importWorker.CancelAsync();
                btnCancel.IsEnabled = false;
            }
        }

        // Handler for clicking the "Export to Excel" button
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dbService == null)
                    throw new InvalidOperationException("Database not connected");

                var filter = new ExportFilter
                {
                    DateFrom = dpDateFrom.SelectedDate,
                    DateTo = dpDateTo.SelectedDate,
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    SurName = txtSurName.Text,
                    City = txtCity.Text,
                    Country = txtCountry.Text
                };

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var data = dbService.GetFilteredData(filter);
                    new ExcelExportService().Export(data, saveFileDialog.FileName);
                    LogMessage("Excel export completed!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Export error: {ex.Message}");
            }
        }

        // Handler for clicking the "Export to XML" button
        private void BtnExportXml_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dbService == null)
                    throw new InvalidOperationException("Database not connected");

                var filter = new ExportFilter
                {
                    DateFrom = dpDateFrom.SelectedDate,
                    DateTo = dpDateTo.SelectedDate,
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    SurName = txtSurName.Text,
                    City = txtCity.Text,
                    Country = txtCountry.Text
                };

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "XML files (*.xml)|*.xml",
                    DefaultExt = ".xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exporter = new XmlExportService();
                    exporter.Export(dbService.GetFilteredData(filter), saveFileDialog.FileName);
                    LogMessage("XML export completed!");
                }
            }
            catch (Exception ex)
            {
                ShowError($"XML export error: {ex.Message}");
            }
        }

        // Method for logging messages
        private void LogMessage(string message)
        {
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\n");
        }

        // Method for displaying error messages
        private void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Event handler for the Exit button click event
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}