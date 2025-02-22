using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class Dashboardinfo
    {
        //public int RecordID { get; set; }
        [Key]
        public int ONCOUNT { get; set; }
        public int OFFCOUNT { get; set; }
        public int NCCOUNT { get; set; }
        public int BYPASSCOUNT { get; set; }
    }
}
