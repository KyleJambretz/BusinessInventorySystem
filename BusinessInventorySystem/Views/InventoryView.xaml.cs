using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using BusinessInventorySystem.Models;
using BusinessInventorySystem.Services;

namespace BusinessInventorySystem.Views
{
    public partial class InventoryView : UserControl
    {
        private readonly InventoryService _service = InventoryService.Instance;
        private readonly ObservableCollection<InventoryItem> _items;
        private readonly ObservableCollection<StockMovement> _movements;
        private readonly ICollectionView _itemsView;
        private readonly ICollectionView _movementsView;

        public InventoryView()
        {
            InitializeComponent();
            _items = _service.Items;
            _movements = _service.Movements;

            _itemsView = CollectionViewSource.GetDefaultView(_items);
            _itemsView.Filter = FilterItems;
            ItemsGrid.ItemsSource = _itemsView;

            _movementsView = CollectionViewSource.GetDefaultView(_movements);
            _movementsView.Filter = FilterMovements;
            _movementsView.SortDescriptions.Add(
                new SortDescription(nameof(StockMovement.Timestamp), ListSortDirection.Descending));
            HistoryGrid.ItemsSource = _movementsView;

            TypeFilter.ItemsSource = new[] { "All Types", "Received", "Sold", "Adjusted", "Returned" };
            TypeFilter.SelectedIndex = 0;

            _service.ItemAdded += (_, _) => UpdateSummary();
            _service.ItemUpdated += (_, _) => UpdateSummary();
            _service.ItemRemoved += (_, _) => UpdateSummary();
            _service.StockChanged += OnStockChanged;

            UpdateSummary();
        }

        private void OnStockChanged(object? sender, StockChangedEventArgs e)
        {
            UpdateSummary();
            if (ItemsGrid.SelectedItem is InventoryItem selected && selected.Sku == e.Item.Sku)
                RefreshItemHistory(selected);
        }

        private void RefreshItemHistory(InventoryItem item)
        {
            ItemHistoryList.ItemsSource = _movements
                .Where(m => m.Sku == item.Sku)
                .OrderByDescending(m => m.Timestamp)
                .ToList();
        }

        private bool FilterItems(object obj)
        {
            if (obj is not InventoryItem item) return false;
            var q = SearchBox.Text?.Trim() ?? "";
            if (q.Length == 0) return true;
            return item.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || item.Sku.Contains(q, StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains(q, StringComparison.OrdinalIgnoreCase)
                || item.Location.Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private bool FilterMovements(object obj)
        {
            if (obj is not StockMovement m) return false;
            if (TypeFilter?.SelectedItem is not string selected || selected == "All Types") return true;
            return m.Type.ToString() == selected;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _itemsView?.Refresh();
            UpdateSummary();
        }

        private void TypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _movementsView?.Refresh();
        }

        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is InventoryItem item)
            {
                DetailPanel.DataContext = item;
                RefreshItemHistory(item);
                DetailPanel.Visibility = Visibility.Visible;
                NoSelectionText.Visibility = Visibility.Collapsed;
            }
            else
            {
                DetailPanel.Visibility = Visibility.Collapsed;
                NoSelectionText.Visibility = Visibility.Visible;
            }
        }

        private void UpdateSummary()
        {
            var visible = _items.Count(FilterItems);
            var low = _items.Count(i => i.StockStatus != "In Stock");
            SummaryText.Text = $"{visible} of {_items.Count} items shown · {low} need attention";
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ItemDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true && dialog.Result is not null)
                ItemsGrid.SelectedItem = dialog.Result;
        }

        private void AdjustStock_Click(object sender, RoutedEventArgs e)
        {
            OpenAdjustStock(ItemsGrid.SelectedItem as InventoryItem);
        }

        private void OpenAdjustStock(InventoryItem? item)
        {
            var dialog = new AdjustStockDialog(item) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
        }

        // Context-menu handlers: the menu item's DataContext is the row the user right-clicked.

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not InventoryItem item) return;
            var dialog = new ItemDialog(item) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
        }

        private void AdjustStockRow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is InventoryItem item)
                OpenAdjustStock(item);
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not InventoryItem item) return;

            var confirm = MessageBox.Show(
                $"Remove {item.Name} ({item.Sku}) from the inventory?\n\nIts movement history will be kept in the activity log.",
                "Remove Item", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
                _service.RemoveItem(item.Sku);
        }

        private void RevertMovement_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.DataContext is not StockMovement movement) return;

            var confirm = MessageBox.Show(
                $"Revert this {movement.Type} of {movement.QuantityChangeDisplay} on {movement.ItemName}?\n\n" +
                "A compensating adjustment will be added to the log.",
                "Revert Movement", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _service.RevertMovement(movement, InventoryService.CurrentUser);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Cannot Revert", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
