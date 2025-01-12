using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace av_motion_api.ViewModels
{
    public class UpdateInventoryViewModel
    {
        [Required]

        public DateTime Supplies_Received_Date { get; set; }

        public bool? Accepted { get; set; } // Nullable bool to indicate acceptance/rejection

        public string? Discrepancies { get; set; }  // Nullable attribute for discrepancies

        public List<ReceivedSupplierOrderLineViewModel> ReceivedOrderLines { get; set; } = new List<ReceivedSupplierOrderLineViewModel>();
    }
}
