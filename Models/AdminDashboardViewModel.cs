using System;
using System.Collections.Generic;

namespace PharmaTrust.Models
{
    public class TopSellingProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CurrentStock { get; set; }
        public bool InStock => CurrentStock > 0;
        public bool IsLowStock => CurrentStock > 0 && CurrentStock <= 5;
    }

    public class DailySalesTrend
    {
        public DateTime Date { get; set; }
        public string DayLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class MonthlySalesTrend
    {
        public int Month { get; set; }
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class AdminDashboardViewModel
    {
        // Global Stock & Catalog KPIs
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProductsCount { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalUnitsInStock { get; set; }
        public int TotalOrdersCount { get; set; }
        public int RxProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }
        public decimal EstimatedStockValue { get; set; }

        // Financial & Cash on Delivery (COD) KPIs
        public decimal RevenueDeliveredToday { get; set; }
        public decimal RevenuePendingToday { get; set; }
        public int OrdersTodayCount { get; set; }

        public decimal RevenueDeliveredMonth { get; set; }
        public decimal RevenuePendingMonth { get; set; }
        public int OrdersMonthCount { get; set; }

        public decimal RevenueDeliveredYear { get; set; }
        public decimal RevenuePendingYear { get; set; }
        public int OrdersYearCount { get; set; }

        public decimal RevenueDeliveredAllTime { get; set; }
        public decimal RevenuePendingAllTime { get; set; }

        public decimal AverageOrderValue { get; set; }

        // Order Status Counts
        public int OrdersPendingCount { get; set; }       // "En attente"
        public int OrdersProcessingCount { get; set; }    // "En préparation"
        public int OrdersShippedCount { get; set; }       // "Expédiée"
        public int OrdersDeliveredCount { get; set; }     // "Livrée"
        public int OrdersCancelledCount { get; set; }     // "Annulée"

        // Best Sellers & Trends
        public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = new();
        public List<DailySalesTrend> DailyTrendLast7Days { get; set; } = new();
        public List<MonthlySalesTrend> MonthlyTrendThisYear { get; set; } = new();

        // Recent Entities
        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<StockMovement> RecentMovements { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
