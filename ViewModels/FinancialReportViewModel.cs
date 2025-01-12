namespace av_motion_api.ViewModels
{
    public class FinancialReportViewModel
    {
        public string Category_Name { get; set; }
        public string Product_Name { get; set; }
        public int Total_Ordered { get; set; }
        public decimal Total_Revenue { get; set; }
        public string Payment_Type_Name { get; set; }
        public decimal Total_Received { get; set; }
        public int NumberOfPayments { get; set; }
        public string Payer { get; set; }
        public int Member_ID { get; set; }
    }
}
