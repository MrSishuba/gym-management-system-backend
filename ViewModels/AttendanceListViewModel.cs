namespace av_motion_api.ViewModels
{
    public class AttendanceListViewModel
    {
        public int attendanceID { get; set; }
        public int numberOfBookings { get; set; }
        public int membersPresent { get; set; }
        public int membersAbsent { get; set; }
        public int member_ID { get; set; }
        public string member_Name { get; set; }
        public string programName { get; set; }
        public int bookingSlot_ID { get; set; }
        public DateTime slotDate { get; set; }
        public DateTime slotTime { get; set; }

      
    }
}
