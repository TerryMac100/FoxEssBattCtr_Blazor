using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlazorBattControl.Models
{
    public class BatteryMode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int SchedualId { get; set; }
        public int TimeSlot { get; set; }
        public int BattMode { get; set; }
    }
}
