namespace av_motion_api.ViewModels
{
    public class OrderReportViewModel
    {
        public string Product_Name { get; set; }
        public int Total_Products_Purchased { get; set; }
        public string Category_Name { get; set; }
        public int Total_Ordered { get; set; }
        public decimal Total_Sales { get; set; }

        public int Order_Date { get; set; }    

    }
}
