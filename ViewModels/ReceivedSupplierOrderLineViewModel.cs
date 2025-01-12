namespace av_motion_api.ViewModels
{
    public class ReceivedSupplierOrderViewModel
    {
        public DateTime Supplies_Received_Date { get; set; }

        public bool? Accepted { get; set; } // Nullable bool to indicate acceptance/rejection

        public string? Discrepancies { get; set; }  // Nullable attribute for discrepancies

        public List<ReceivedSupplierOrderLineViewModel> Received_Supplier_Order_Lines { get; set; }
    }

    public class ReceivedSupplierOrderLineViewModel
    {
        public int Product_ID { get; set; }
        public int Received_Supplies_Quantity { get; set; }
        public int Supplier_Order_Line_ID { get; set; }
    }
}
