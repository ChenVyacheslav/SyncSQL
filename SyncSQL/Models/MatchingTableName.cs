using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Models
{
    [Table("tbl_MatchingTableNames")]
    public class MatchingTableName
    {
        public Guid ID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public Guid? CreatedByID { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedByID { get; set; }

        public string ServerTableName { get; set; }

        public string ClientTableName { get; set; }

        public string Description { get; set; }

        public Guid? ServerTableID { get; set; }

        public int? Index { get; set; }

        public int? ScopeIgnore { get; set; }
    }
}
