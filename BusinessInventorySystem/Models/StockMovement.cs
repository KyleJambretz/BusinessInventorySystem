using System;

namespace BusinessInventorySystem.Models
{
    public enum MovementType
    {
        Received,
        Sold,
        Adjusted,
        Returned
    }

    public class StockMovement
    {
        public DateTime Timestamp { get; set; }
        public string Sku { get; set; } = "";
        public string ItemName { get; set; } = "";
        public MovementType Type { get; set; }
        public int QuantityChange { get; set; }
        public int QuantityAfter { get; set; }
        public string User { get; set; } = "";
        public string Note { get; set; } = "";

        public string QuantityChangeDisplay =>
            QuantityChange > 0 ? $"+{QuantityChange}" : QuantityChange.ToString();
    }
}
