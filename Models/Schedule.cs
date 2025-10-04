namespace BlazorBattControl.Models
{
    public class Schedule
    {
        public int Id { get; set; }
        public int SchedualId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinSoc { get; set; }
        public int MinDischargeSoc { get; set; }    
        public int DischargePower { get; set; }
    }
}
