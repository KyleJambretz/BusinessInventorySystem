using System;
using System.Collections.ObjectModel;
using System.Linq;
using BusinessInventorySystem.Models;

namespace BusinessInventorySystem.Services
{
    /// <summary>
    /// Single source of truth for inventory state. Views bind to Items/Movements and
    /// subscribe to the events; all mutations go through the methods here so history
    /// gets recorded, listeners stay in sync, and changes are autosaved to disk.
    /// </summary>
    public class InventoryService
    {
        public static InventoryService Instance { get; } = new();

        public ObservableCollection<InventoryItem> Items { get; } = new();
        public ObservableCollection<StockMovement> Movements { get; } = new();

        public event EventHandler<ItemEventArgs>? ItemAdded;
        public event EventHandler<ItemEventArgs>? ItemUpdated;
        public event EventHandler<ItemEventArgs>? ItemRemoved;
        public event EventHandler<StockChangedEventArgs>? StockChanged;
        public event EventHandler? DataLoaded;

        /// <summary>Suppresses autosave while Load() is populating the collections.</summary>
        private bool _loading;

        private InventoryService() { }

        public static string CurrentUser => Environment.UserName.ToLowerInvariant();

        public InventoryItem? FindBySku(string sku) =>
            Items.FirstOrDefault(i => i.Sku.Equals(sku, StringComparison.OrdinalIgnoreCase));

        public void AddItem(InventoryItem item, string user = "", string note = "")
        {
            if (string.IsNullOrWhiteSpace(item.Sku))
                throw new ArgumentException("SKU is required.", nameof(item));
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Name is required.", nameof(item));
            if (FindBySku(item.Sku) is not null)
                throw new InvalidOperationException($"An item with SKU '{item.Sku}' already exists.");
            if (item.Quantity < 0)
                throw new ArgumentException("Quantity cannot be negative.", nameof(item));

            Items.Add(item);
            if (item.Quantity > 0)
            {
                RecordMovement(item, MovementType.Received, item.Quantity,
                    string.IsNullOrWhiteSpace(user) ? CurrentUser : user,
                    string.IsNullOrWhiteSpace(note) ? "Initial stock" : note);
            }
            ItemAdded?.Invoke(this, new ItemEventArgs(item));
            Autosave();
        }

        /// <summary>
        /// Call after editing an item's properties so listeners are notified and the
        /// change is persisted. Quantity changes should go through AdjustStock instead.
        /// </summary>
        public void UpdateItem(InventoryItem item)
        {
            if (!Items.Contains(item))
                throw new InvalidOperationException($"Item '{item.Sku}' is not in the inventory.");
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Name is required.", nameof(item));

            ItemUpdated?.Invoke(this, new ItemEventArgs(item));
            Autosave();
        }

        public bool RemoveItem(string sku)
        {
            var item = FindBySku(sku);
            if (item is null) return false;

            Items.Remove(item);
            // Movement history is kept intentionally — it's an audit log of what happened.
            ItemRemoved?.Invoke(this, new ItemEventArgs(item));
            Autosave();
            return true;
        }

        /// <summary>
        /// Applies a quantity change to an item, records the movement, and raises StockChanged.
        /// </summary>
        public StockMovement AdjustStock(string sku, int quantityChange, MovementType type, string user, string note = "")
        {
            var item = FindBySku(sku)
                ?? throw new InvalidOperationException($"No item with SKU '{sku}'.");
            if (quantityChange == 0)
                throw new ArgumentException("Quantity change cannot be zero.", nameof(quantityChange));
            if (item.Quantity + quantityChange < 0)
                throw new InvalidOperationException(
                    $"Cannot remove {-quantityChange} — only {item.Quantity} on hand.");

            item.Quantity += quantityChange;
            var movement = RecordMovement(item, type, quantityChange,
                string.IsNullOrWhiteSpace(user) ? CurrentUser : user, note);
            StockChanged?.Invoke(this, new StockChangedEventArgs(item, movement));
            Autosave();
            return movement;
        }

        /// <summary>
        /// Undoes a movement by recording a compensating adjustment. The original
        /// entry stays in the log — history is append-only.
        /// </summary>
        public StockMovement RevertMovement(StockMovement movement, string user)
        {
            if (!Movements.Contains(movement))
                throw new InvalidOperationException("Movement is not in the log.");

            var note = $"Revert of {movement.Type} on {movement.Timestamp:MMM d, yyyy h:mm tt}";
            return AdjustStock(movement.Sku, -movement.QuantityChange, MovementType.Adjusted, user, note);
        }

        private StockMovement RecordMovement(InventoryItem item, MovementType type, int quantityChange, string user, string note)
        {
            var movement = new StockMovement
            {
                Timestamp = DateTime.Now,
                Sku = item.Sku,
                ItemName = item.Name,
                Type = type,
                QuantityChange = quantityChange,
                QuantityAfter = item.Quantity,
                User = user,
                Note = note
            };
            Movements.Add(movement);
            return movement;
        }

        // ---- Persistence ----

        /// <summary>Loads state from disk, or seeds sample data on first run.</summary>
        public void Load()
        {
            _loading = true;
            try
            {
                var snapshot = InventoryFileStore.Load();
                if (snapshot is null)
                {
                    SeedSampleData();
                }
                else
                {
                    Items.Clear();
                    Movements.Clear();
                    foreach (var item in snapshot.Items) Items.Add(item);
                    foreach (var movement in snapshot.Movements) Movements.Add(movement);
                }
            }
            finally
            {
                _loading = false;
            }
            DataLoaded?.Invoke(this, EventArgs.Empty);
        }

        public void Save()
        {
            InventoryFileStore.Save(new InventorySnapshot
            {
                Items = Items.ToList(),
                Movements = Movements.ToList()
            });
        }

        private void Autosave()
        {
            if (!_loading)
                Save();
        }

        private void SeedSampleData()
        {
            Items.Add(new InventoryItem { Sku = "WID-001", Name = "Widget Standard", Category = "Widgets", Quantity = 142, ReorderLevel = 25, UnitPrice = 4.99m, Location = "Aisle 1, Bin 4" });
            Items.Add(new InventoryItem { Sku = "WID-002", Name = "Widget Deluxe", Category = "Widgets", Quantity = 18, ReorderLevel = 25, UnitPrice = 9.99m, Location = "Aisle 1, Bin 5" });
            Items.Add(new InventoryItem { Sku = "GAD-010", Name = "Gadget Pro", Category = "Gadgets", Quantity = 64, ReorderLevel = 15, UnitPrice = 24.50m, Location = "Aisle 2, Bin 1" });
            Items.Add(new InventoryItem { Sku = "GAD-011", Name = "Gadget Mini", Category = "Gadgets", Quantity = 0, ReorderLevel = 10, UnitPrice = 12.00m, Location = "Aisle 2, Bin 2" });
            Items.Add(new InventoryItem { Sku = "CBL-100", Name = "Cable 2m USB-C", Category = "Cables", Quantity = 310, ReorderLevel = 50, UnitPrice = 6.25m, Location = "Aisle 3, Bin 7" });

            var now = DateTime.Now;
            Movements.Add(new StockMovement { Timestamp = now.AddHours(-2), Sku = "WID-002", ItemName = "Widget Deluxe", Type = MovementType.Sold, QuantityChange = -4, QuantityAfter = 18, User = "kyle", Note = "Order #1042" });
            Movements.Add(new StockMovement { Timestamp = now.AddHours(-5), Sku = "WID-001", ItemName = "Widget Standard", Type = MovementType.Sold, QuantityChange = -12, QuantityAfter = 142, User = "kyle", Note = "Order #1041" });
            Movements.Add(new StockMovement { Timestamp = now.AddDays(-1), Sku = "GAD-011", ItemName = "Gadget Mini", Type = MovementType.Sold, QuantityChange = -6, QuantityAfter = 0, User = "sam", Note = "Order #1038" });
            Movements.Add(new StockMovement { Timestamp = now.AddDays(-1).AddHours(-3), Sku = "CBL-100", ItemName = "Cable 2m USB-C", Type = MovementType.Received, QuantityChange = 100, QuantityAfter = 310, User = "sam", Note = "PO-2231" });
            Movements.Add(new StockMovement { Timestamp = now.AddDays(-2), Sku = "GAD-010", ItemName = "Gadget Pro", Type = MovementType.Returned, QuantityChange = 2, QuantityAfter = 64, User = "kyle", Note = "Customer return" });
            Movements.Add(new StockMovement { Timestamp = now.AddDays(-3), Sku = "WID-001", ItemName = "Widget Standard", Type = MovementType.Adjusted, QuantityChange = -3, QuantityAfter = 154, User = "kyle", Note = "Cycle count correction" });
            Movements.Add(new StockMovement { Timestamp = now.AddDays(-4), Sku = "WID-002", ItemName = "Widget Deluxe", Type = MovementType.Received, QuantityChange = 20, QuantityAfter = 22, User = "sam", Note = "PO-2229" });
        }
    }
}
