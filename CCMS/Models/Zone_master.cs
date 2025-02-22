using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class Zone_master
    {
        [Key]
        public int RecordID { get; set; }
        public string? Zone { get; set; }
        public string? Ward { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
    }
}
