namespace av_motion_api.ViewModels
{
    public class ViewWorkoutViewModel
    {
        public int Workout_ID { get; set; }
        public string Workout_Name { get; set; }

        public string Workout_Description { get; set; }
        public string workoutCategory { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }
        public int Workout_Category_ID { get; set; }
    }
}
