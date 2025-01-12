namespace av_motion_api.ViewModels
{
    public class LessonPlanViewModel
    {
        public int lessonPlan_ID { get; set; }
        public List<int> workoutID { get; set; }
        public List <string>  workouts { get; set; }
        public string program_Description { get; set; }
        public string lessonPlanName { get; set; }
        //public string Duration { get; set; }
    }
}
