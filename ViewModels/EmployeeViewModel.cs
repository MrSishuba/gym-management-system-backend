namespace av_motion_api.ViewModels
{
    public class EmployeeViewModel : UserViewModel
    {
        public DateTime Employment_Date { get; set; } = DateTime.UtcNow;
        public int Hours_Worked { get; set; } = 0;
        public int Employee_Type_ID { get; set; }
        public int? Shift_ID { get; set; } = null;
    }

    public class UserEmployeeViewModel
    {
        public int employee_ID { get; set; }
        public string employee_name { get; set; }
    }
}
