using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class LiveReport
    {
        [Key]
        public int RecordID { get; set; }
        public string UID { get; set; }
        public string IMEI_no { get; set; }
        public DateTime? DateTime { get; set; }
        public string? Zone { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNo { get; set; }
        public string? Mode { get; set; }
        public string? RelayStatus { get; set; }
        public double? RVolt { get; set; }
        public double? RCurr { get; set; }
        public double? RKW { get; set; }
        public double? RPF { get; set; }
        public string? Error { get; set; }
        public int? Phase { get; set; }
        public double? Energy { get; set; }
    }
}
