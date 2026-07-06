using System;
using System.Globalization;
using System.Windows;
using BusinessInventorySystem.Models;
using BusinessInventorySystem.Services;

namespace BusinessInventorySystem.Views
{
    /// <summary>
    /// Add/edit dialog for inventory items. In edit mode the SKU is locked (movements
    /// reference it) and quantity is locked (quantity changes go through Adjust Stock
    /// so history stays accurate).
    /// </summary>
    public partial class ItemDialog : Window
    {
        private readonly InventoryItem? _existing;

        /// <summary>The added or edited item, set when the dialog closes with OK.</summary>
        public InventoryItem? Result { get; private set; }

        public ItemDialog(InventoryItem? existing = null)
        {
            InitializeComponent();
            _existing = existing;

            if (existing is null)
            {
                SkuBox.Focus();
                return;
            }

            Title = "Edit Item";
            TitleText.Text = "Edit Item";
            SaveButton.Content = "Save";

            SkuBox.Text = existing.Sku;
            SkuBox.IsEnabled = false;
            NameBox.Text = existing.Name;
            CategoryBox.Text = existing.Category;
            QuantityBox.Text = existing.Quantity.ToString();
            QuantityBox.IsEnabled = false;
            QuantityBox.ToolTip = "Use Adjust Stock to change the quantity.";
            ReorderBox.Text = existing.ReorderLevel.ToString();
            PriceBox.Text = existing.UnitPrice.ToString("0.00", CultureInfo.CurrentCulture);
            LocationBox.Text = existing.Location;
            NameBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text))
            {
                ShowError("Name is required.");
                return;
            }
            if (!int.TryParse(ReorderBox.Text, out var reorderLevel) || reorderLevel < 0)
            {
                ShowError("Reorder level must be a non-negative whole number.");
                return;
            }
            if (!decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price < 0)
            {
                ShowError("Unit price must be a non-negative number.");
                return;
            }

            try
            {
                Result = _existing is null ? AddNew(reorderLevel, price) : SaveExisting(reorderLevel, price);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                ShowError(ex.Message);
                return;
            }

            DialogResult = true;
        }

        private InventoryItem AddNew(int reorderLevel, decimal price)
        {
            if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity < 0)
                throw new ArgumentException("Quantity must be a non-negative whole number.");

            var item = new InventoryItem
            {
                Sku = SkuBox.Text.Trim().ToUpperInvariant(),
                Name = NameBox.Text.Trim(),
                Category = CategoryBox.Text.Trim(),
                Quantity = quantity,
                ReorderLevel = reorderLevel,
                UnitPrice = price,
                Location = LocationBox.Text.Trim()
            };
            InventoryService.Instance.AddItem(item);
            return item;
        }

        private InventoryItem SaveExisting(int reorderLevel, decimal price)
        {
            _existing!.Name = NameBox.Text.Trim();
            _existing.Category = CategoryBox.Text.Trim();
            _existing.ReorderLevel = reorderLevel;
            _existing.UnitPrice = price;
            _existing.Location = LocationBox.Text.Trim();
            InventoryService.Instance.UpdateItem(_existing);
            return _existing;
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
