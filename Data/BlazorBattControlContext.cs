using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public DbSet<BlazorBattControl.Models.Schedule> Schedule { get; set; } = default!;
    }
}
