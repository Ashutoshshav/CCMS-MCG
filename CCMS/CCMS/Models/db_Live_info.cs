using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class db_Live_info
    {
        [Key]
        public string IMEI_no { get; set; }
        public int DUR { get; set; }
        public float P10 { get; set; }
    }
}
