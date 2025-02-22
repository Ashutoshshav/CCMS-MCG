using CCMS.Models;
using Microsoft.EntityFrameworkCore;

namespace CCMS.Service
{
    public class ReportsDbContext : DbContext
    {
        public ReportsDbContext(DbContextOptions<ReportsDbContext> options) : base(options) { }

        public DbSet<LiveReport> LiveReport { get; set; }
        public DbSet<DayReport> DayReport { get; set; }
        public DbSet<CCMS.Models.IMEI_Master> IMEI_Master { get; set; } = default!;
    }
}
