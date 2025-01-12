namespace av_motion_api.ViewModels
{
    public class WriteoffViewModel
    {
        public int Write_Off_ID { get; set; }
        public DateTime Date { get; set; }
        public string Write_Off_Reason { get; set; }
        public int Inventory_ID { get; set; }
        public int Write_Off_Quantity { get; set; }
        public string Inventory_Item_Name { get; set; }
    }
}
