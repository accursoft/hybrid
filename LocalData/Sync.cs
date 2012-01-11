using System.Data.SqlClient;
using LocalData.Properties;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data.SqlServer;

namespace LocalData
{
    public static class Sync
    {
        public static void Synchronize()
        {
            using (var local = new SqlConnection(Settings.Default.LocalData))
            using (var proxy = new SyncProxy(Settings.Default.Scope))
                new SyncOrchestrator() {
                    LocalProvider = new SqlSyncProvider(Settings.Default.Scope, local),
                    RemoteProvider = proxy,
                    Direction = SyncDirectionOrder.DownloadAndUpload
                }.Synchronize();
        }

        public static void Provision()
        {
            using (var local = new SqlConnection(Settings.Default.LocalData))
            using (var proxy = new SyncProxy(Settings.Default.Scope))
                new SqlSyncScopeProvisioning(local, proxy.GetScopeDescription()).Apply();
        }

        public static bool Provisioned()
        {
            using (var local = new SqlConnection(Settings.Default.LocalData))
                return new SqlSyncScopeProvisioning(local).ScopeExists(Settings.Default.Scope);
        }
    }
}