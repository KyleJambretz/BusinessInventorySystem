using System;
using BusinessInventorySystem.Models;

namespace BusinessInventorySystem.Services
{
    public class ItemEventArgs : EventArgs
    {
        public InventoryItem Item { get; }

        public ItemEventArgs(InventoryItem item) => Item = item;
    }

    public class StockChangedEventArgs : EventArgs
    {
        public InventoryItem Item { get; }
        public StockMovement Movement { get; }

        public StockChangedEventArgs(InventoryItem item, StockMovement movement)
        {
            Item = item;
            Movement = movement;
        }
    }
}
