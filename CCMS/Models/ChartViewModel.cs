using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class ChartViewModel
    {
        [Key]
        public int RecordID { get; set; }
        public List<ChartModel> FirstChart { get; set; }
        public List<ChartModel> SecondChart { get; set; }
    }
}
