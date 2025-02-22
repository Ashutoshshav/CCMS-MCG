using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class IMEI_Master
    {
        [Key]
        public string UID { get; set; }
        public int? SNO { get; set; }
        public string? IMEI_no { get; set; }
        public string? Location { get; set; }
        public int? LiveDevice { get; set; }
        public string? Response { get; set; }
        public string? Zone { get; set; }
        public string? Ward { get; set; }
        public string? Status { get; set; }
        public string? SIM_No { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public int? Phase { get; set; }
        public string? NoOfStreetlight { get; set; }
        public double? FullLoad { get; set; }
        public DateTime ResponseDTime { get; set; }
    }
}