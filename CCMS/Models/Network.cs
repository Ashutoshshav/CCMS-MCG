using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class Network
    {
        [Key]
        public decimal RecordID { get; set; }
        public int DUR { get; set; }
        public string IMEI_no { get; set; }
    }
}
