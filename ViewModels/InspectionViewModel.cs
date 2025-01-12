namespace av_motion_api.ViewModels
{
    public class InspectionViewModel
    {
        public int Inspection_ID { get; set; }
        public DateTime Inspection_Date { get; set; }
        public string inspection_notes { get; set; }
        public int? equipment_id { get; set; }
        public int inspection_type_id { get; set; }
        public int inspection_status_id { get; set; }
        public int? inventory_id { get; set; }
        public string equipment_name { get; set; }
        public string inspection_status { get; set; }
        public string inventory_category { get; set; }
        public string inventory_name { get; set; }
        public string inspection_type_name { get; set; }
    }
}
