namespace av_motion_api.ViewModels
{
    public class InventoryViewModel
    {
        public int inventoryID { get; set; }
        public string category { get; set; }
        public string itemName { get; set; }
        public int quantity { get; set; }
        public string photo { get; set; }
        public int? supplierID { get; set; }
        public string? supplierName { get; set; }
        public int? received_supplier_order_id { get; set; }
    }
}
