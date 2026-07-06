using System.Windows;
using BusinessInventorySystem.Views;

namespace BusinessInventorySystem
{
    public partial class MainWindow : Window
    {
        private readonly InventoryView _inventoryView = new();
        private readonly AboutView _aboutView = new();

        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = _inventoryView;
        }

        private void NavInventory_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent != null)
                MainContent.Content = _inventoryView;
        }

        private void NavAbout_Checked(object sender, RoutedEventArgs e)
        {
            if (MainContent != null)
                MainContent.Content = _aboutView;
        }
    }
}
