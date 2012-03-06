using System;
using System.Data.SqlClient;
using System.IO;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data.SqlServer;

using ApexSql.Diff.Structure;
using ApexSql.Diff;

using SyncClient.Properties;
using SyncClient.SyncService;
using SyncClient.SchemaService;

namespace SyncClient
{
    public static class Sync
    {
        public static void Synchronize()
        {
            using (var local = new SqlConnection(Settings.Default.Local)) {

                local.Open();

                //check for schema update
                using (var proxy = new SchemaServiceClient()) {
                    byte version = proxy.GetSchemaVersion();
                    if (version > Settings.Default.SchemaVersion) {

                        //prepare comparison
                        string schema = Path.GetTempFileName();
                        File.WriteAllText(schema, proxy.GetSchema());
                        
                        StructureProject project = new StructureProject(schema, new ConnectionProperties(Settings.Default.ApexSqlServer, Settings.Default.ApexSqlDb));
                        project.ComparisonOptions.IgnoreIdentitySeedAndIncrement = project.ComparisonOptions.IgnorePrimaryKeys = true;

                        project.MappedObjects.ExcludeAllFromComparison();
                        project.MappedTables.IncludeInComparison(Settings.Default.ApexSqlTables.Split(','));
                        project.ComparedObjects.IncludeAllInSynchronization();

                        //enable trigger to adjust sync scope
                        new SqlCommand(Settings.Default.EnableTrigger, local).ExecuteNonQuery();

                        //sync
                        var errors = project.ExecuteSynchronizationScript();
                        File.Delete(schema);
                        if (errors.Length > 0)
                            throw new ApplicationException(string.Join("\n", errors));

                        //put trigger back to sleep
                        new SqlCommand(Settings.Default.DisableTrigger, local).ExecuteNonQuery();

                        Settings.Default.SchemaVersion = version;
                        Settings.Default.Save();
                    }
                }

                using (var proxy = new SyncProxy(Settings.Default.Scope)) {

                    //provision if needed
                    if (!IsProvisioned())
                        new SqlSyncScopeProvisioning(local, proxy.GetScopeDescription()).Apply();

                    //synchronise
                    new SyncOrchestrator() {
                        LocalProvider = new SqlSyncProvider(Settings.Default.Scope, local),
                        RemoteProvider = proxy,
                        Direction = SyncDirectionOrder.DownloadAndUpload
                    }.Synchronize();
                }

                //do we have an ID range?
                if (Settings.Default.MaxID == 0) {
                    using (var client = new SyncServiceClient()) {
                        var range = client.GetIdRange(Environment.MachineName);
                        Settings.Default.MinID = range.Min;
                        Settings.Default.MaxID = range.Max;
                        Settings.Default.Save();
                    }
                }

                //reseed the client
                Repository.Repository.Reseed(Settings.Default.MinID, Settings.Default.MaxID, local);
            }
        }

        public static bool IsProvisioned()
        {
            using (var local = new SqlConnection(Settings.Default.Local))
                return new SqlSyncScopeProvisioning(local).ScopeExists(Settings.Default.Scope);
        }
    }
}