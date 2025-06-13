using MahApps.Metro.Controls;

namespace Exporter
{
    public partial class MainWindow : MetroWindow
    {
        // ViewModel instance for the window.
        public MainViewModel ViewModel { get; }

        // Constructor initializing components and ViewModel.
        public MainWindow()
        {
            InitializeComponent();
            var repository = new DbRepository();
            var csvParser = new CsvParserService();

            ViewModel = new MainViewModel(repository, csvParser, this);
            DataContext = ViewModel;
        }
    }
}
