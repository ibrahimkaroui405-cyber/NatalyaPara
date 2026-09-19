using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmaTrust.Models
{
    public class Order
    {
        [Key]
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? DeliveryNotes { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public List<OrderItem> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "En attente"; // "En attente", "En préparation", "Expédiée", "Livrée", "Annulée"
        public string PaymentMethod { get; set; } = "Paiement à la livraison (Espèces)";
        public bool StockDeducted { get; set; } = true;
    }

    public class OrderItem
    {
        [Key]
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        [NotMapped]
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
