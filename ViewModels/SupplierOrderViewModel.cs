namespace av_motion_api.ViewModels
{
    public class SupplierOrderViewModel
    {
        public int Supplier_Order_ID { get; set; }
        public DateTime Date { get; set; }
        public string Supplier_Order_Details { get; set; }
        public decimal Total_Price { get; set; }
        public int Supplier_ID { get; set; }
        public string Supplier_Name { get; set; }
        public int Owner_ID { get; set; }
        public int Status { get; set; }
        public ICollection<SupplierOrderLineViewModel> OrderLines { get; set; }
    }

    public class UpdateSupplierOrderStatusViewModel
    {
        public int Supplier_Order_ID { get; set; }
        public int Status { get; set; } // Boolean to indicate acceptance/rejection
    }
}