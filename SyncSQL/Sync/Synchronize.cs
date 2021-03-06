﻿using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using SyncSQL.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
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


            //((SqlSyncProvider)syncOrchestrator.RemoteProvider).SyncProgress += new EventHandler<DbSyncProgressEventArgs>(serverSyncProvider_SyncProgress);
            //((SqlSyncProvider)syncOrchestrator.LocalProvider).SyncProgress += new EventHandler<DbSyncProgressEventArgs>(clientSyncProvider_SyncProgress);

            ((SqlSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);


            Console.WriteLine("Start Time: " + DateTime.Now);
            var syncStats = syncOrchestrator.Synchronize();

            // print statistics
            //Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
            Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
            Console.WriteLine("Total Changes Downloaded: " + syncStats.DownloadChangesTotal);
            Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
            Console.WriteLine(String.Empty);
        }

        //CLIENT! -> SERVER (COLUMNS)
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

                    var serverNullColumns = tableColumns.Where(t => t.ServerColumnNullCheck == 1).ToList();

                    if (serverNullColumns.Any())
                    {
                        for (int j = 0; j < dataTable.Rows.Count; j++)
                        {
                            DataRow row = dataTable.Rows[j];
                            if (row.RowState != DataRowState.Deleted)
                            {
                                foreach (var serverNullColumn in serverNullColumns) //Передаем на сервер вместо пустых значений null (NULL на сервере)
                                {
                                    if ((serverNullColumn.ServerColumnFieldType == 1 || serverNullColumn.ServerColumnFieldType == 2) && Convert.ToInt32(row[serverNullColumn.ServerColumnName]) == 0)
                                        row[serverNullColumn.ServerColumnName] = DBNull.Value;
                                    else if ((serverNullColumn.ServerColumnFieldType == 3 || serverNullColumn.ServerColumnFieldType == 8) && String.IsNullOrEmpty(row[serverNullColumn.ServerColumnName].ToString()))
                                        row[serverNullColumn.ServerColumnName] = DBNull.Value;
                                }
                            }
                        }
                    }
                }
            }
        }

        //SERVER! -> CLIENT (COLUMNS)
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

                    var serverNullColumns = tableColumns.Where(t => t.ServerColumnNullCheck == 1).ToList();

                    if (serverNullColumns.Any())
                    {
                        for (int j = 0; j < dataTable.Rows.Count; j++)
                        {
                            DataRow row = dataTable.Rows[j];
                            if (row.RowState != DataRowState.Deleted)
                            {
                                foreach (var serverNullColumn in serverNullColumns) //Передаем на клиент вместо null значения по умолчанию (NULL на сервере)
                                {
                                    if ((serverNullColumn.ServerColumnFieldType == 1 || serverNullColumn.ServerColumnFieldType == 2) && row[serverNullColumn.ClientColumnName] == DBNull.Value)
                                        row[serverNullColumn.ClientColumnName] = 0;
                                    else if ((serverNullColumn.ServerColumnFieldType == 3 || serverNullColumn.ServerColumnFieldType == 8) && row[serverNullColumn.ClientColumnName] == DBNull.Value)
                                        row[serverNullColumn.ClientColumnName] = "";
                                }
                            }
                        }
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

        private void serverSyncProvider_SyncProgress(object sender, DbSyncProgressEventArgs e)
        {
            string message = "Server Progress: ";

            switch (e.Stage)
            {
                case DbSyncStage.ApplyingInserts:
                    message += "Applying insert for table: " + e.TableProgress.TableName;
                    message += "[" + e.TableProgress.Inserts.ToString() + "|" + e.TableProgress.Updates.ToString() + "|" + e.TableProgress.Deletes.ToString() + "]";
                    message += "(Applied:" + e.TableProgress.ChangesApplied.ToString() + "/Pending:" + e.TableProgress.ChangesPending.ToString() +
                    "/Failed:" + e.TableProgress.ChangesFailed.ToString() + "/Total:" + e.TableProgress.TotalChanges.ToString() + ")";
                    break;
            }

            Console.WriteLine(message);
        }

        private void clientSyncProvider_SyncProgress(object sender, DbSyncProgressEventArgs e)
        {
            string message = "Client Progress: ";

            switch (e.Stage)
            {
                case DbSyncStage.ApplyingInserts:
                    message += "Applying insert for table: " + e.TableProgress.TableName;
                    message += "[" + e.TableProgress.Inserts.ToString() + "|" + e.TableProgress.Updates.ToString() + "|" + e.TableProgress.Deletes.ToString() + "]";
                    message += "(Applied:" + e.TableProgress.ChangesApplied.ToString() + "/Pending:" + e.TableProgress.ChangesPending.ToString() +
                    "/Failed:" + e.TableProgress.ChangesFailed.ToString() + "/Total:" + e.TableProgress.TotalChanges.ToString() + ")";
                    break;
            }

            Console.WriteLine(message);
        }

    }
}
