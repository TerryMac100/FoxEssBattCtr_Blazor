using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorBattControl.Models
{
    public class Schedule
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; } = "Default Schedule";
        public string Description { get; set; } = "Default Description";
        public int MinSoc { get; set; } = 20;
        public int MinDischargeSoc { get; set; } = 40;  
        public int DischargePower { get; set; } = 3600; // in Watts
    }
}
