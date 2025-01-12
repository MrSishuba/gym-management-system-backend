using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Audit_Trail
    {
        [Key]
        public int Audit_Trail_ID { get; set; }

        public string Transaction_Type { get; set; }           // 'INSERT', 'UPDATE', 'DELETE'

        public string? Critical_Data { get; set; }   // Critical data related to the transaction (e.g., amount, quantity)

        public string Changed_By { get; set; }       // User who made the change

        public string Table_Name { get; set; }        // Name of the table being audited

        public DateTime Timestamp { get; set; }      // Timestamp of the change
    }
}
