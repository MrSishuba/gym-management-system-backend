namespace av_motion_api.ViewModels
{
    public class BookingViewModel
    {
      
        public int booking_ID { get; set; }
        public string lessonPlanName { get; set; }
        public int lessonPlanID { get; set; }
        public DateTime date { get; set; }
        public DateTime time { get; set; }
        public int timeSlot_ID { get; set; }
        public string instructorName { get; set; }

       
    }
}
