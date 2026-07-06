using System;
using System.Windows;
using System.Windows.Controls;
using BusinessInventorySystem.Models;
using BusinessInventorySystem.Services;

namespace BusinessInventorySystem.Views
{
    public partial class AdjustStockDialog : Window
    {
        public AdjustStockDialog(InventoryItem? preselected = null)
        {
            InitializeComponent();

            ItemCombo.ItemsSource = InventoryService.Instance.Items;
            ItemCombo.SelectedItem = preselected ?? (InventoryService.Instance.Items.Count > 0
                ? InventoryService.Instance.Items[0]
                : null);

            TypeCombo.ItemsSource = Enum.GetValues<MovementType>();
            TypeCombo.SelectedItem = MovementType.Adjusted;

            ChangeBox.Focus();
        }

        private void ItemCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnHandText.Text = ItemCombo.SelectedItem is InventoryItem item
                ? $"{item.Quantity} on hand"
                : "";
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (ItemCombo.SelectedItem is not InventoryItem item)
            {
                ShowError("Select an item.");
                return;
            }
            if (TypeCombo.SelectedItem is not MovementType type)
            {
                ShowError("Select a movement type.");
                return;
            }
            if (!int.TryParse(ChangeBox.Text, out var change))
            {
                ShowError("Quantity change must be a whole number (negative to remove stock).");
                return;
            }

            try
            {
                InventoryService.Instance.AdjustStock(item.Sku, change, type,
                    InventoryService.CurrentUser, NoteBox.Text.Trim());
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ShowError(ex.Message);
                return;
            }

            DialogResult = true;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
