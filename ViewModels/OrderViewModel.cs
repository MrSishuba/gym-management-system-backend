namespace av_motion_api.ViewModels
{
    public class OrderViewModel
    {
        public int Order_ID { get; set; }
        public int Member_ID { get; set; }
        public DateTime Order_Date { get; set; }
        public int Order_Status_ID { get; set; }
        public bool IsCollected { get; set; }
        public decimal Total_Price { get; set; }
        public List<OrderLineViewModel> OrderLines { get; set; }
    }

    public class OrderLineViewModel
    {
        public int Order_Line_ID { get; set; }
        public int Product_ID { get; set; }
        public string Product_Name { get; set; }
        public int Quantity { get; set; }
        public decimal Unit_Price { get; set; }
    }
}
