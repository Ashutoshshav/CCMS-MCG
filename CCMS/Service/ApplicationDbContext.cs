using CCMS.Models;
using Microsoft.EntityFrameworkCore;

namespace CCMS.Service
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Zone_master>()
            .HasKey(e => e.RecordID);
        }

        public DbSet<Zone_master> Zone_master { get; set; }
        public DbSet<VisLive> VisLive { get; set; }
        public DbSet<IMEI_Master> IMEI_Master { get; set; }
        public DbSet<ChartModel> ChartModel { get; set; }
        public DbSet<ChartViewModel> ChartViewModel { get; set; }
        public DbSet<Login_Master> Login_Master { get; set; }
        public DbSet<Network> Network { get; set; }
        public DbSet<NetworkSts> NetworkSts { get; set; }
    }
}
