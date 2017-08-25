using Microsoft.Synchronization.Data.SqlServer;
using SyncSQL.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deprovision
{
    class Program
    {
        static void Main(string[] args)
        {
            SqlConnection clientConn = new SqlConnection("Data Source=WIN01-DEV02; Initial Catalog=BPMonline; Integrated Security=True");
            SqlConnection serverConn = new SqlConnection("Data Source=WIN01-DEV; Initial Catalog=Test1706; Integrated Security=True");

            SqlSyncScopeDeprovisioning serverScopeDeprovisioning = new SqlSyncScopeDeprovisioning(serverConn);
            SqlSyncScopeDeprovisioning clientScopeDeprovisioning = new SqlSyncScopeDeprovisioning(clientConn);

            using (var db = new SystemContext())
            {
                var sсhemeScopes = db.SсhemeScopes.OrderByDescending(s => s.CreatedOn).ToList();

                foreach (var sсhemeScope in sсhemeScopes)
                {
                    Console.WriteLine("Delete scope: " + sсhemeScope.Name);
                    clientScopeDeprovisioning.DeprovisionScope(sсhemeScope.Name);
                    serverScopeDeprovisioning.DeprovisionScope(sсhemeScope.Name);
                    db.SсhemeScopes.Remove(sсhemeScope);
                }
                db.SaveChanges();
            }
        }
    }
}
