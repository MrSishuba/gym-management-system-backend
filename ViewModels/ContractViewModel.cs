namespace av_motion_api.ViewModels
{
    public class ContractViewModel
    {
        public DateTime Subscription_Date { get; set; }
        public DateTime Expiry_Date { get; set; }
        public DateTime? Approval_Date { get; set; }
        public bool Terms_Of_Agreement { get; set; }
        public bool Approval_Status { get; set; }
        public string Approval_By { get; set; }
        public string Filepath { get; set; }
        public int Contract_Type_ID { get; set; }
        public int Payment_Type_ID { get; set; }
        public int Member_ID { get; set; }
        public int Employee_ID { get; set; }
        public int? Owner_ID { get; set; }
    }
}
