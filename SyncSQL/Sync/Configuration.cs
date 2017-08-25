using SyncSQL.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Sync
{
    public class Configuration
    {
        private SqlConnection clientConn;
        private SqlConnection serverConn;
        private string scopeName;
        private const string SYNC_SETTING_CHANGE_FLAG = "SyncSettingChangeFlag";

        public Configuration()
        {
            clientConn = new SqlConnection("Data Source=WIN01-DEV02; Initial Catalog=BPMonline; Integrated Security=True");
            serverConn = new SqlConnection("Data Source=WIN01-DEV; Initial Catalog=Test1706; Integrated Security=True");
        }

        public SqlConnection ServerSqlConnection
        {
            get { return serverConn; }
        }

        public SqlConnection ClientSqlConnection
        {
            get { return clientConn; }
        }

        public string ScopeName
        {
            get { return scopeName;  }
        }

        public void GenerateScopeName()
        {
            using (var db = new SystemContext())
            {
                var currentSchemeScope = db.SсhemeScopes.Where(s => s.IsCurrent).FirstOrDefault();
                var syncSetting = db.SystemSetting.First(s => s.Code == SYNC_SETTING_CHANGE_FLAG);

                if (currentSchemeScope == null || Convert.ToBoolean(syncSetting.BooleanValue.Value))
                {
                    if (currentSchemeScope != null)
                        currentSchemeScope.IsCurrent = false;

                    syncSetting.BooleanValue = 0;

                    scopeName = "SchemeScope_" + DateTime.Now.ToString();
                    db.SсhemeScopes.Add(new SchemeScope
                    {
                        CreatedOn = DateTime.Now,
                        IsCurrent = true,
                        Name = scopeName,
                    });
                    db.SaveChanges();
                }
                else
                {
                    scopeName = currentSchemeScope.Name;
                }
            }
        }

    }
}
