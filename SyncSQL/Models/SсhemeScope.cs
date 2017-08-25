using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Models
{
    [Table("tbl_SchemeScopes")]
    public class SchemeScope
    {
        public SchemeScope()
        {
            ID = Guid.NewGuid();
        }
        public Guid ID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public Guid? CreatedByID { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedByID { get; set; }

        public string Name { get; set; }

        public bool IsCurrent { get; set; }
    }
}
