using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BusinessInventorySystem.Models
{
    public class InventoryItem : INotifyPropertyChanged
    {
        private string _sku = "";
        private string _name = "";
        private string _category = "";
        private int _quantity;
        private int _reorderLevel;
        private decimal _unitPrice;
        private string _location = "";

        public string Sku { get => _sku; set => SetField(ref _sku, value); }
        public string Name { get => _name; set => SetField(ref _name, value); }
        public string Category { get => _category; set => SetField(ref _category, value); }
        public int ReorderLevel { get => _reorderLevel; set { if (SetField(ref _reorderLevel, value)) OnPropertyChanged(nameof(StockStatus)); } }
        public decimal UnitPrice { get => _unitPrice; set => SetField(ref _unitPrice, value); }
        public string Location { get => _location; set => SetField(ref _location, value); }

        public int Quantity
        {
            get => _quantity;
            set { if (SetField(ref _quantity, value)) OnPropertyChanged(nameof(StockStatus)); }
        }

        public string StockStatus =>
            Quantity == 0 ? "Out of Stock" :
            Quantity <= ReorderLevel ? "Low Stock" : "In Stock";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
