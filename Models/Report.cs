using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace av_motion_api.Models
{
    public class Report
    {
       [Key]
       public int Report_ID { get; set; }

       [Required]
       public string Report_Name { get; set;}

       [Required]
       public string Report_Description { get; set;}

       [Required]
       public DateTime Generated_Date { get; set;}


    }
}
