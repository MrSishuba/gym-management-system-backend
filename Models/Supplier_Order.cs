using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace av_motion_api.Models
{
    public class Supplier_Order
    {
        [Key]
        public int Supplier_Order_ID { get; set; }

        //[Required]
        public DateTime Date { get; set; }

        //[Required]
        public string? Supplier_Order_Details { get; set; }

        //[Required]
        public decimal Total_Price { get; set; }

        public int Status { get; set; }

        public int Supplier_ID { get; set; }

        [ForeignKey(nameof(Supplier_ID))]
        public Supplier Supplier { get; set; }

        public int Owner_ID { get; set; }

        [ForeignKey(nameof(Owner_ID))]
        public Owner Owner { get; set; }

        public ICollection<Supplier_Order_Line> Supplier_Order_Lines { get; set; }
    }
}