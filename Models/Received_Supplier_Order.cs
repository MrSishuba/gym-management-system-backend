using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Received_Supplier_Order
    {
        [Key]
        public int Received_Supplier_Order_ID { get; set; }

        [Required]
        public DateTime Supplies_Received_Date { get; set; }

        public bool Accepted { get; set; } // Indicates if the order was accepted

        public string? Discrepancies { get; set; }  // Nullable attribute for discrepancies

        public ICollection<Received_Supplier_Order_Line> Received_Supplier_Order_Lines { get; set; }
    }
}