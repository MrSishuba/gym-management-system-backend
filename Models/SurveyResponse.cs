using System.ComponentModel.DataAnnotations;
namespace av_motion_api.Models
{
    public class SurveyResponse
    {
        [Key]
        public int SurveyResponseId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string MembershipStatus { get; set; }

        [Required]
        public string AgeGroup { get; set; }

        // Membership and Booking Experience
        [Required]
        public string BookingSatisfaction { get; set; }

        public bool BookingIssues { get; set; }

        public string BookingIssueDetails { get; set; }

        // Purchasing Products
        [Required]
        public string PurchaseFrequency { get; set; }

        [Required]
        public string ProductSatisfaction { get; set; }

        public bool NewProductInterest { get; set; }

        public string NewProductDetails { get; set; }

        // General Feedback
        [Required]
        public int RecommendGym { get; set; }

        public string Suggestions { get; set; }

        // Contact Consent
        public bool ContactConsent { get; set; }
    }
}
