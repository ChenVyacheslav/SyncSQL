using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Models
{
    [Table("tbl_SystemSetting")]
    public class SystemSetting
    {
        public Guid ID { get; set; }

        public string Code { get; set; }

        public string StringValue { get; set; }

        public DateTime? DateTimeValue { get; set; }

        public int? IntegerValue { get; set; }

        public float? FloatValue { get; set; }

        public int? BooleanValue { get; set; }

        public Guid? DictionaryRecordID { get; set; }

        public string EnumItemID { get; set; }

        public int? IsCaching { get; set; }

        public Guid ValueTypeId { get; set; }

        public string Caption { get; set; }

        public string Description { get; set; }

        public Guid? GroupID { get; set; }

        public DateTime? CreatedOn { get; set; }

        public Guid? CreatedByID { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedByID { get; set; }

        public string DatasetCode { get; set; }

        public string EnumCode { get; set; }

    }
}
