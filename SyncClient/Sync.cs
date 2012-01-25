using System;
using System.Data.SqlClient;
using SyncClient.Properties;
using SyncClient.SyncService;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data.SqlServer;

namespace SyncClient
{
    public static class Sync
    {
        public static void Synchronize()
        {
            using (var local = new SqlConnection(Settings.Default.Local)) {

                local.Open();

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