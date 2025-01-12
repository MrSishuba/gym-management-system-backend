namespace av_motion_api.ViewModels
{
    public class InspectionReportViewModel
    {
        public string Inventory_Item_Name { get; set; }
        public int NumberOfWriteOffs { get; set; }
        public int TotalQuantityWrittenOff { get; set; }
        public string Equipment_Name { get; set; }
        public int Number_Of_Inspections { get; set; }
        public DateTime Inspection_Date { get; set; }
        public string Inspection_Type { get; set; }
        public string Inspection_Notes { get; set; }

    }
}
