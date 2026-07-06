using System.Collections.Generic;

namespace BusinessInventorySystem.Models
{
    /// <summary>
    /// Serializable container for everything that gets written to disk.
    /// Add new top-level state here (settings, users, etc.) as the app grows.
    /// </summary>
    public class InventorySnapshot
    {
        public List<InventoryItem> Items { get; set; } = new();
        public List<StockMovement> Movements { get; set; } = new();
    }
}
