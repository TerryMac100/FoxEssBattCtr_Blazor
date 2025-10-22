using Microsoft.EntityFrameworkCore;
using BlazorBattControl.Models;

namespace BlazorBattControl.Data
{
    public class BlazorBattControlContext : DbContext
    {
        public BlazorBattControlContext (DbContextOptions<BlazorBattControlContext> options)
            : base(options)
        {
        }

        public DbSet<Schedule> Schedule { get; set; } = default!;
        public DbSet<BatteryMode> Mode { get; set; } = default!;

        public DbSet<AppDbSettings> AppDbSettings { get; set; } = default!;
    }
}
