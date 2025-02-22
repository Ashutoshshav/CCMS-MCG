using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class ReportingEmails
    {
        [Key]
        public int RecordID { get; set; }
        public string? EmailID { get; set; }
    }
}
