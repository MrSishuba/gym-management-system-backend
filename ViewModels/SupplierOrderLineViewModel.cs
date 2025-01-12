namespace av_motion_api.ViewModels
{
    public class SupplierOrderLineViewModel
    {
        public int Supplier_Order_Line_ID { get; set; }
        public int Product_ID { get; set; }
        public string Product_Name { get; set; }
        public int Supplier_Quantity { get; set; }

        public decimal? Purchase_Price { get; set; }
    }
}
