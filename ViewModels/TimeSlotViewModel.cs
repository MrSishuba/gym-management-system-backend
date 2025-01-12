namespace av_motion_api.ViewModels
{
    public class TimeSlotViewModel
    {
        public int timeSlotID { get; set; }
        public DateTime date { get; set; }
        public DateTime time { get; set; }
        public bool Availability { get; set; }
        public int employee_ID { get; set; }
        public int lesson_Plan_ID { get; set; }
    }
}
