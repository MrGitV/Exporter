using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Threading;
using MahApps.Metro.Controls;

namespace Exporter
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MetroWindow mainWindow;
        private readonly IRepository repository;
        private readonly CsvParserService csvParser;
        private CancellationTokenSource cancellationTokenSource;

        private string csvPath;
        private string server;
        private string database;
        private DateTime? dateFrom;
        private DateTime? dateTo;
        private string firstName;
        private string lastName;
        private string surName;
        private string city;
        private string country;
        private string log;
        private CancellationTokenSource exportCancellationTokenSource;

        // Constructor initializing dependencies and commands.
        public MainViewModel(IRepository repository, CsvParserService csvParser, MetroWindow window)
        {
            this.repository = repository;
            this.csvParser = csvParser;
            mainWindow = window;

            BrowseCommand = new RelayCommand(Browse);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcel, () => IsConnected && !IsBusy);
            ExportXmlCommand = new AsyncRelayCommand(ExportXml, () => IsConnected && !IsBusy);
            ImportCommand = new AsyncRelayCommand(Import, () => IsConnected && !IsBusy);
            CancelCommand = new RelayCommand(CancelImport, () => IsBusy);
            ConnectCommand = new AsyncRelayCommand(Connect, () => !IsBusy);
            ExitCommand = new RelayCommand(ExitApp);
        }

        // Path to the CSV file selected by the user.
        public string CsvPath
        {
            get => csvPath;
            set => SetField(ref csvPath, value);
        }

        // Database server name input by the user.
        public string Server
        {
            get => server;
            set => SetField(ref server, value);
        }

        // Database name input by the user.
        public string Database
        {
            get => database;
            set => SetField(ref database, value);
        }

        // Filter start date for export.
        public DateTime? DateFrom
        {
            get => dateFrom;
            set => SetField(ref dateFrom, value);
        }

        // Filter end date for export.
        public DateTime? DateTo
        {
            get => dateTo;
            set => SetField(ref dateTo, value);
        }

        // Filter first name for export.
        public string FirstName
        {
            get => firstName;
            set => SetField(ref firstName, value);
        }

        // Filter last name for export.
        public string LastName
        {
            get => lastName;
            set => SetField(ref lastName, value);
        }

        // Filter surname for export.
        public string SurName
        {
            get => surName;
            set => SetField(ref surName, value);
        }

        // Filter city for export.
        public string City
        {
            get => city;
            set => SetField(ref city, value);
        }

        // Country filter property
        public string Country
        {
            get => country;
            set => SetField(ref country, value);
        }

        // Log text shown to the user with timestamps and messages.
        public string Log
        {
            get => log;
            set => SetField(ref log, value);
        }

        // Commands bound to UI buttons
        public ICommand BrowseCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand ImportCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportXmlCommand { get; }
        public ICommand ExitCommand { get; }
        public RelayCommand CancelCommand { get; }

        private bool isBusy;

        // Indicates if an operation (import/export) is in progress
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (SetField(ref isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    CancelCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isConnected;

        // Indicates if the application is connected to the database
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (SetField(ref isConnected, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Adds a timestamped message to the log
        private void LogMessage(string message)
        {
            Log += $"{DateTime.Now:HH:mm:ss} - {message}\n";
        }

        // Opens a file dialog for the user to select a CSV file.
        private void Browse()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Select CSV File"
            };
            if (dialog.ShowDialog() == true)
            {
                CsvPath = dialog.FileName;
            }
        }

        // Shows a message dialog asynchronously using MahApps.Metro dialogs.
        private async Task ShowMessageAsync(string title, string message)
        {
            await mainWindow.ShowMessageAsync(title, message);
        }

        // Connects to the database using the provided server and database names.
        private async Task Connect()
        {
            if (IsBusy) return;

            IsBusy = true;
            IsConnected = false;

            if (!ValidateInputs(out var error))
            {
                await ShowMessageAsync("Validation Error", error);
                IsBusy = false;
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(Server))
                    throw new ArgumentException("Server is required");
                if (string.IsNullOrWhiteSpace(Database))
                    throw new ArgumentException("Database is required");

                repository.SetConnectionString(Server, Database);
                await repository.TestConnectionAsync(Server, Database);

                await ShowMessageAsync("Success", "Connected to database successfully");
                IsConnected = true;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                await ShowMessageAsync("Connection Error", $"Failed to connect to database:\n{ex.Message}");
            }
            finally
            {
                IsBusy = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // Recursively gets full message of exception including inner exceptions.
        private string GetFullExceptionMessage(Exception ex)
        {
            if (ex == null) return string.Empty;
            return ex.Message + (ex.InnerException != null ? "\nInner exception: " + GetFullExceptionMessage(ex.InnerException) : string.Empty);
        }

        // Imports CSV data asynchronously into the database.
        private async Task Import()
        {
            IsBusy = true;
            if (string.IsNullOrWhiteSpace(CsvPath) || !File.Exists(CsvPath))
            {
                await ShowMessageAsync("Error", "Please select a valid CSV file path.");
                return;
            }
            cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await repository.ImportDataAsync(
                    csvParser.ParseFileAsync(CsvPath),
                    new Progress<string>(msg => Log += $"{DateTime.Now:HH:mm:ss} - {msg}\n"),
                    cancellationTokenSource.Token);
            }
            catch (FormatException fex)
            {
                await ShowMessageAsync("Error", $"CSV format error: {fex.Message}");
            }
            catch (OperationCanceledException)
            {
                Log += $"{DateTime.Now:HH:mm:ss} - Import cancelled by user\n";
            }
            catch (Exception ex)
            {
                string fullMessage = GetFullExceptionMessage(ex);
                await ShowMessageAsync("Import failed", fullMessage);
            }
            finally
            {
                IsBusy = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
            }
        }

        // Validates required inputs for CSV path, server, and database.
        private bool ValidateInputs(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(CsvPath) || !File.Exists(CsvPath))
            {
                errorMessage = "Please select a valid CSV file.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Server))
            {
                errorMessage = "Server is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(Database))
            {
                errorMessage = "Database is required.";
                return false;
            }
            errorMessage = null;
            return true;
        }

        // Exports filtered data to an Excel file asynchronously.
        private async Task ExportExcel()
        {
            if (IsBusy) return;
            IsBusy = true;
            exportCancellationTokenSource = new CancellationTokenSource();

            try
            {
                LogMessage("Starting export to Excel...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var filter = new ExportFilter
                {
                    DateFrom = DateFrom,
                    DateTo = DateTo,
                    FirstName = FirstName,
                    LastName = LastName,
                    SurName = SurName,
                    City = City,
                    Country = Country
                };

                var data = await repository.GetFilteredDataAsync(filter);
                var asyncData = data.ToAsyncEnumerable();

                var saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var progress = new Progress<string>(msg => Log += $"{DateTime.Now:HH:mm:ss} - {msg}\n");
                    await new ExcelExportService().ExportAsync(asyncData, saveDialog.FileName, progress, exportCancellationTokenSource.Token);
                    LogMessage($"Excel export completed: {saveDialog.FileName} (elapsed {stopwatch.Elapsed.TotalSeconds:F1} s)");
                }
                else
                {
                    LogMessage("Excel export canceled by user.");
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("Excel export cancelled by user.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Error", $"Excel export failed: {ex.Message}");
                LogMessage($"Excel export error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                exportCancellationTokenSource.Dispose();
                exportCancellationTokenSource = null;
            }
        }

        // Exports filtered data to an XML file asynchronously.
        private async Task ExportXml()
        {
            if (IsBusy) return;
            IsBusy = true;
            exportCancellationTokenSource = new CancellationTokenSource();

            try
            {
                LogMessage("Starting export to XML...");

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var filter = new ExportFilter
                {
                    DateFrom = DateFrom,
                    DateTo = DateTo,
                    FirstName = FirstName,
                    LastName = LastName,
                    SurName = SurName,
                    City = City,
                    Country = Country
                };

                var data = await repository.GetFilteredDataAsync(filter);
                var asyncData = data.ToAsyncEnumerable();

                var saveDialog = new SaveFileDialog
                {
                    Filter = "XML files (*.xml)|*.xml",
                    DefaultExt = ".xml",
                    FileName = $"Export_{DateTime.Now:yyyyMMdd_HHmmss}.xml"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var progress = new Progress<string>(msg => Log += $"{DateTime.Now:HH:mm:ss} - {msg}\n");
                    await new XmlExportService().ExportAsync(asyncData, saveDialog.FileName, progress, exportCancellationTokenSource.Token);
                    LogMessage($"XML export completed: {saveDialog.FileName} (elapsed {stopwatch.Elapsed.TotalSeconds:F1} s)");
                }
                else
                {
                    LogMessage("XML export canceled by user.");
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage("XML export cancelled by user.");
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Error", $"XML export failed: {ex.Message}");
                LogMessage($"XML export error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                exportCancellationTokenSource.Dispose();
                exportCancellationTokenSource = null;
            }
        }

        // Event raised when a property value changes.
        public event PropertyChangedEventHandler PropertyChanged;

        // Raises PropertyChanged event for the specified property.
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // Sets the field value and raises PropertyChanged if value changed.
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Cancels the ongoing import operation.
        private void CancelImport()
        {
            cancellationTokenSource?.Cancel();
            exportCancellationTokenSource?.Cancel();
            Log += $"{DateTime.Now:HH:mm:ss} - Operation cancelled\n";
        }

        // Exits the application.
        private void ExitApp()
        {
            Application.Current.Shutdown();
        }
    }
}