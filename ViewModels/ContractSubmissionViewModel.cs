namespace av_motion_api.ViewModels
{
    public class ContractSubmissionViewModel
    {
        public string Member_Name { get; set; }
        public IFormFile File { get; set; }
        public IFormFile ConsentFormFile { get; set; } // Consent form file
    }
}
