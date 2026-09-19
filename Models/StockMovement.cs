namespace PharmaTrust.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int QuantityChange { get; set; } // Positive for addition/restock, negative for sale/deduction
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public string Reason { get; set; } = string.Empty; // e.g., "Commande Client #CMD-...", "Réapprovisionnement Fournisseur"
        public string ReferenceType { get; set; } = "Adjustment"; // "Order", "Restock", "Adjustment", "Cancellation"
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
