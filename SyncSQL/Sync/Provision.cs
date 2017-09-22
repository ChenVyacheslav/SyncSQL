using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using SyncSQL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Sync
{
    public class Provision
    {
        private Configuration configuration;
        private List<MatchingTableName> tables;
        private List<MatchingColumnName> columns;

        public Provision(Configuration configuration)
        {
            this.configuration = configuration;
            using (var db = new SystemContext())
            {
                tables = db.MatchingTableNames.Where(m => m.ScopeIgnore == null || m.ScopeIgnore.Value == 0).OrderBy(m => m.Index).ToList();
                columns = db.MatchingColumnNames.OrderBy(m => m.CreatedOn).ToList();
            }
        }

        public void Run()
        {
            string scopeName = configuration.ScopeName;
            
            //SERVER PROVISION
            var serverScopeDesc = new DbSyncScopeDescription(scopeName);
            serverScopeDesc = UpdateServerScopeDesc(serverScopeDesc);
            var serverProvision = new SqlSyncScopeProvisioning(configuration.ServerSqlConnection, serverScopeDesc);

            if (!serverProvision.ScopeExists(scopeName))
            {
                serverProvision.PopulateFromScopeDescription(serverScopeDesc);
                serverProvision.Apply();
            }
            else
            {
                UpdateExistingScopeProvision(serverProvision, configuration.ServerSqlConnection, true);
            }

            //CLIENT PROVISION
            var clientScopeDesc = new DbSyncScopeDescription(scopeName);
            clientScopeDesc = UpdateClientScopeDesc(clientScopeDesc);

            var clientProvision = new SqlSyncScopeProvisioning(configuration.ClientSqlConnection, clientScopeDesc);

            if (!clientProvision.ScopeExists(scopeName))
            {
                clientProvision.PopulateFromScopeDescription(clientScopeDesc);
                clientProvision.Apply();
            }
            else
            {
                UpdateExistingScopeProvision(clientProvision, configuration.ClientSqlConnection, false);
            }

            using (var db = new SystemContext())
            {
                var changeTables = db.MatchingTableNames.Where(m => m.ScopeIgnore == null || m.ScopeIgnore.Value == 0).OrderBy(m => m.Index).ToList();
                foreach (var changeTable in changeTables)
                {
                    changeTable.InScope = 1;
                }
                db.SaveChanges();
            }

        }

        private void UpdateExistingScopeProvision(SqlSyncScopeProvisioning provision, SqlConnection conn, bool isServer)
        {
            string alterScopeSql = string.Empty;
            provision.SetCreateProceduresDefault(DbSyncCreationOption.Create);
            provision.SetUseBulkProceduresDefault(true);

            provision.SetCreateTrackingTableDefault(DbSyncCreationOption.CreateOrUseExisting);
            var provisioningScript = provision.Script();
            //alterScopeSql = provision.Script();

            var stringBuilder = new StringBuilder();
            var changedTables = tables.Where(t => t.InScope.Value == 1).ToList();

            foreach (var changedTable in changedTables)
            {
                var tableName = isServer ? changedTable.ServerTableName : changedTable.ClientTableName;
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_bulkinsert];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_bulkupdate];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_bulkdelete];");
                stringBuilder.AppendLine("DROP TYPE [" + tableName + "_BulkType];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_selectchanges];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_selectrow];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_insert];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_update];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_delete];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_insertmetadata];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_updatemetadata];");
                stringBuilder.AppendLine("DROP PROCEDURE [" + tableName + "_deletemetadata];");
            }

            // append the sync provisioning script after the drop statements
            alterScopeSql = stringBuilder.Append(provisioningScript).ToString();

            int x = alterScopeSql.IndexOf("N'<SqlSyncProviderScopeConfiguration");
            int y = alterScopeSql.IndexOf("</SqlSyncProviderScopeConfiguration>");
            var configEntry = alterScopeSql.Substring(x, y - x) + "</SqlSyncProviderScopeConfiguration>'";

            x = alterScopeSql.IndexOf("- BEGIN Add scope");
            y = alterScopeSql.IndexOf("- END Add Scope");
            alterScopeSql = alterScopeSql.Remove(x, y - x);

            alterScopeSql = alterScopeSql.Replace("scope_status = 'C'", "config_data=" + configEntry);

            x = alterScopeSql.IndexOf("WHERE [config_id] =");
            alterScopeSql = alterScopeSql.Remove(x, alterScopeSql.Length - x);

            alterScopeSql = alterScopeSql
                + " WHERE [config_id] = (SELECT scope_config_id FROM scope_info WHERE sync_scope_name='"
                + configuration.ScopeName + "')";

            using (var connection = new SqlConnection(conn.ConnectionString))
            {
                connection.Open();
                string[] commands = alterScopeSql.Split(new string[] { "GO\r\n", "GO ", "GO\t", "GO" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var c in commands)
                {
                    var command = new SqlCommand(c, connection);
                    command.ExecuteNonQuery();
                }
            }
        }

        private DbSyncScopeDescription UpdateServerScopeDesc(DbSyncScopeDescription serverScopeDesc)
        {
            foreach (var table in tables)
            {
                var serverTableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(table.ServerTableName, configuration.ServerSqlConnection);
                serverTableDesc.GlobalName = table.ServerTableName;
                serverScopeDesc.Tables.Add(serverTableDesc);

                var tableColumns = columns.Where(c => c.MatchingTableNameID == table.ID).Select(c => c.ServerColumnName).ToList();
                var columnsForRemove = serverScopeDesc.Tables[table.ServerTableName].Columns.Select(c => c.UnquotedName).Except(tableColumns).ToList();
                foreach (var columnForRemove in columnsForRemove)
                {
                    serverScopeDesc.Tables[table.ServerTableName].Columns.Remove(serverScopeDesc.Tables[table.ServerTableName].Columns[columnForRemove]);
                }
            }
            return serverScopeDesc;
        }

        private DbSyncScopeDescription UpdateClientScopeDesc(DbSyncScopeDescription clientScopeDesc)
        {
            foreach (var table in tables)
            {
                var clientTableDesc = SqlSyncDescriptionBuilder.GetDescriptionForTable(table.ClientTableName, configuration.ClientSqlConnection);
                clientTableDesc.GlobalName = table.ServerTableName;
                clientScopeDesc.Tables.Add(clientTableDesc);

                var tableColumns = columns.Where(c => c.MatchingTableNameID == table.ID).Select(c => c.ClientColumnName).ToList();
                var columnsForRemove = clientScopeDesc.Tables[table.ServerTableName].Columns.Select(c => c.UnquotedName).Except(tableColumns).ToList();

                foreach (var columnForRemove in columnsForRemove)
                {
                    clientScopeDesc.Tables[table.ServerTableName].Columns.Remove(clientScopeDesc.Tables[table.ServerTableName].Columns[columnForRemove]);
                }
            }
            return clientScopeDesc;
        }

    }
}
