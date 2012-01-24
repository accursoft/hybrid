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

                using (var proxy = new SyncProxy(Settings.Default.Scope))
                    new SyncOrchestrator() {
                        LocalProvider = new SqlSyncProvider(Settings.Default.Scope, local),
                        RemoteProvider = proxy,
                        Direction = SyncDirectionOrder.DownloadAndUpload
                    }.Synchronize();

                using (var reseed = new SqlCommand(Settings.Default.ReseedID, local)) {
                    if (Settings.Default.MaxID == 0) GetIdRange();

                    reseed.Parameters.AddWithValue("min", Settings.Default.MinID);
                    reseed.Parameters.AddWithValue("max", Settings.Default.MaxID);
                    reseed.ExecuteNonQuery();
                }
            }
        }

        static void GetIdRange()
        {
            using (var client = new SyncServiceClient()) {
                var range = client.GetIdRange(Environment.MachineName);
                Settings.Default.MinID = range.Min;
                Settings.Default.MaxID = range.Max;
                Settings.Default.Save();
            }
        }

        public static void Provision()
        {
            using (var local = new SqlConnection(Settings.Default.Local))
            using (var proxy = new SyncProxy(Settings.Default.Scope))
                new SqlSyncScopeProvisioning(local, proxy.GetScopeDescription()).Apply();
        }

        public static bool IsProvisioned()
        {
            using (var local = new SqlConnection(Settings.Default.Local))
                return new SqlSyncScopeProvisioning(local).ScopeExists(Settings.Default.Scope);
        }
    }
}