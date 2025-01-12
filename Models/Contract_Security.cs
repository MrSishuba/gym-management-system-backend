using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.Models
{
    public class Contract_Security
    {
        [Key]
        public int Contract_Security_ID { get; set; } // Primary Key
        public string HashedPassword { get; set; } // Stored hashed password
        public DateTime LastUpdated { get; set; } // Optional: track when the password was last updated
    }
}
