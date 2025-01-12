using System.Reflection.Metadata;

namespace av_motion_api.ViewModels
{
    public class TimeSlotCalanderViewModel
    {
        public int TimeSlotId { get; set; }
        public DateTime SlotTime { get; set; }
        public DateTime SlotDate { get; set; }
        public bool Availability { get; set; }
        public string Description { get; set; }
        public string ProgramName { get; set; }
        public string Employee_Name { get; set; }
        public int NumberOfBookings { get; set; }
        public int Employee_ID { get; set; }
        public int Lesson_Plan_ID { get; set; }
      
    }
}
