namespace av_motion_api.ViewModels
{
    public class InventoryProductViewModel
    {
        public string category { get; set; }
        public string itemName { get; set; }
        public int quantity { get; set; }
        public string photo { get; set; }
        public int supplierID { get; set; }
        public int received_supplier_order_id { get; set; }
        public string productName { get; set; }
        public string description { get; set; }
        public DateTime Create_Date { get; set; }
        public DateTime Last_Update_Date { get; set; }
        public bool IsActive { get; set; }
        public string Size { get; set; }
        public string proudctCategory { get; set; }
        public int Product_Category_ID { get; set; }
    }
}
