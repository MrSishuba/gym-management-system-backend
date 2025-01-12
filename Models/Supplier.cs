using System.ComponentModel.DataAnnotations;
namespace av_motion_api.Models
{
    public class Supplier
    {
      [Key]
      public int Supplier_ID { get; set; }

      [Required, StringLength(100)]
      public string Name { get; set; }

      [Required,StringLength(15)]
      
      public string Contact_Number { get; set; }

     [Required, StringLength(50)]

     public string Email_Address { get; set; }


     [Required, StringLength(150)]

     public string Physical_Address { get; set; }
    }
}
