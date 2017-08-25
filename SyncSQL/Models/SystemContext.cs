using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Models
{
    public class SystemContext : DbContext
    {
        public DbSet<MatchingTableName> MatchingTableNames { get; set; }
        public DbSet<MatchingColumnName> MatchingColumnNames { get; set; }
        public DbSet<SchemeScope> SсhemeScopes { get; set; }
        public DbSet<SystemSetting> SystemSetting { get; set; }

    }
}
