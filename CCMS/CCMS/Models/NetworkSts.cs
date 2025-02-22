using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class NetworkSts
    {
        [Key]
        public int RecordID { get; set; }
        public int DeviceOffline { get; set; }
        public int DeviceResponseDur { get; set; }
        public int Device_NA { get; set; }
        public int ONHour { get; set; }
        public int ONMin { get; set; }
        public int OFFHour { get; set; }
        public int OFFMin { get; set; }
        public int? Loader { get; set; }
    }
}
