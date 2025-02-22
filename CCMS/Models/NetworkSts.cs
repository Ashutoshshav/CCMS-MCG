using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class NetworkSts
    {
        [Key]
        public int RecordID { get; set; }
        public int DeviceOffline { get; set; }
        public int Device_NA { get; set; }
    }
}
