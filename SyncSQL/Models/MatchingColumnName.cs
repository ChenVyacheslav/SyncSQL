using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Models
{   
    [Table("tbl_MatchingColumnNames")]
    public class MatchingColumnName
    {
        public Guid ID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public Guid? CreatedByID { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedByID { get; set; }

        public Guid MatchingTableNameID { get; set; }

        public MatchingTableName MatchingTableName { get; set; }

        public string ServerColumnName { get; set; }

        public string ClientColumnName { get; set; }

        public int? ServerColumnNullCheck { get; set; }

        public int? ClientColumnNullCheck { get; set; }

        public string IsNullDefaultValue { get; set; }

        public int? ServerColumnFieldType { get; set; }
    }
}
