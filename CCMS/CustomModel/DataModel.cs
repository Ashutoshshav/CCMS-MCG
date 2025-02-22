namespace CCMS.CustomModel
{
    public class DataModel
    {
        public CCMS.Models.VisLive visLive_s { get; set; }

        public CCMS.Models.IMEI_Master IMEI_Master_s { get; set; }
        public IEnumerable<CCMS.Models.IMEI_Master> IMEI_MasterList { get; set; }
        public List<string> zones { get; set; }
        public List<string> wards { get; set; }
    }
}
