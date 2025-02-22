using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class ChartModel
    {
        [Key]
        public int RecordID { get; set; }
        public int ON { get; set; }
        public int OFF { get; set; }
        public int NC { get; set; }
        public int TotalNoOfStreetlight { get; set; }
        public double P10 { get; set; }
    }
}
