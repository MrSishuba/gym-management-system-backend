namespace av_motion_api.ViewModels
{
    public class InventoryReportViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int QuantityInStock { get; set; }
        public decimal TotalStockValue { get; set; }
        public int QuantityWrittenOff { get; set; } 
        public decimal TotalWriteOffValue { get; set; }
        public int QuantityOrdered { get; set; }
        public decimal TotalValue { get; set; }
    }
}
