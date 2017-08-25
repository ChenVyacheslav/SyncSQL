using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using SyncSQL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncSQL.Sync
{
    public class Synchronize
    {
        private Configuration configuration;
        private List<MatchingTableName> tables;
        private List<MatchingColumnName> columns;

        public Synchronize(Configuration configuration)
        {
            this.configuration = configuration;
            using (var db = new SystemContext())
            {
                tables = db.MatchingTableNames.Where(m => m.ScopeIgnore == null || m.ScopeIgnore.Value == 0).ToList();
                columns = db.MatchingColumnNames.ToList();
            }
        }

        public void Run()
        {
            var syncOrchestrator = new SyncOrchestrator();

            var localProvider = new SqlSyncProvider(configuration.ScopeName, configuration.ClientSqlConnection);
            var remoteProvider = new SqlSyncProvider(configuration.ScopeName, configuration.ServerSqlConnection);

            localProvider.ChangesSelected += new EventHandler<DbChangesSelectedEventArgs>(localProvider_ChangesSelected);
            remoteProvider.ChangesSelected += new EventHandler<DbChangesSelectedEventArgs>(remoteProvider_ChangesSelected);

            syncOrchestrator.LocalProvider = localProvider;
            syncOrchestrator.RemoteProvider = remoteProvider;

            syncOrchestrator.Direction = SyncDirectionOrder.UploadAndDownload;
            ((SqlSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);

            var syncStats = syncOrchestrator.Synchronize();

            // print statistics
            Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
        }

        //CLIENT -> SERVER (COLUMNS)
        private void localProvider_ChangesSelected(object sender, DbChangesSelectedEventArgs e)
        {
            foreach (var table in tables)
            {
                if (e.Context.DataSet.Tables.Contains(table.ServerTableName))
                {
                    var dataTable = e.Context.DataSet.Tables[table.ServerTableName];
                    var tableColumns = columns.Where(c => c.MatchingTableNameID == table.ID).ToList();
                    foreach (var tableColumn in tableColumns)
                    {
                        dataTable.Columns[tableColumn.ClientColumnName].ColumnName = tableColumn.ServerColumnName;
                    }
                }
            }
        }

        //SERVER -> CLIENT (COLUMNS)
        private void remoteProvider_ChangesSelected(object sender, DbChangesSelectedEventArgs e)
        {
            foreach (var table in tables)
            {
                if (e.Context.DataSet.Tables.Contains(table.ServerTableName))
                {
                    var dataTable = e.Context.DataSet.Tables[table.ServerTableName];
                    var tableColumns = columns.Where(c => c.MatchingTableNameID == table.ID).ToList();

                    foreach (var tableColumn in tableColumns)
                    {
                        dataTable.Columns[tableColumn.ServerColumnName].ColumnName = tableColumn.ClientColumnName;
                    }
                }
            }
        }

        private void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            File.WriteAllText("Errors.txt",
                DateTime.Now.ToString() + '\n' +
                e.Conflict.Type.ToString() + ": " + e.Error.ToString());

        }
    }
}
