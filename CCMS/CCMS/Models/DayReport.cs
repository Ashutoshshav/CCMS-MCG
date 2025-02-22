using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class DayReport
    {
        [Key]
        public int RecordID { get; set; }
        public string UID { get; set; }
        public string IMEI_no { get; set; }
        public DateTime? RDateTime { get; set; }
        public DateTime? DDateTime { get; set; }
        public string? Zone { get; set; }
        public string? Ward { get; set; }
        public string? PhoneNo { get; set; }
        public string? Mode { get; set; }
        public string? OnTime { get; set; }
        public double? RVolt { get; set; }
        public double? YVolt { get; set; }
        public double? BVolt { get; set; }
        public double? RKW { get; set; }
        public double? YKW { get; set; }
        public double? BKW { get; set; }
        public double? RPF { get; set; }
        public double? YPF { get; set; }
        public double? BPF { get; set; }
        public int? Phase { get; set; }
        public double? OpenReading { get; set; }
        public double? CloseReading { get; set; }
        public double? UnitConsumed { get; set; }
    }
}
