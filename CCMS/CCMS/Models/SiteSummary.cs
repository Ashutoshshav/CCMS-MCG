using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class SiteSummary
    {
        [Key]
        public string UID { get; set; }
        public int? SNO { get; set; }
        public string? Location { get; set; }
        public string? Zone { get; set; }
        public string? Ward { get; set; }
        public int? LIGHTSTS { get; set; }
    }
}
