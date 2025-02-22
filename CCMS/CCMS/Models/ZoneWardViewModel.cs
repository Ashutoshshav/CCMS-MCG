using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CCMS.Models
{
    public class ZoneWardViewModel
    {
        public SelectList Zones { get; set; }
        public SelectList Wards { get; set; }
        public int SelectedZoneId { get; set; }
        public int SelectedWardId { get; set; }
    }
}
