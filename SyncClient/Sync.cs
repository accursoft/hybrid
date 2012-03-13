using System;
using System.Data.SqlClient;
using System.Linq;

using ApexSql.Diff;
using ApexSql.Diff.Structure;
using ApexSql.Diff.Data;

using SyncClient.Properties;
using SyncClient.SchemaService;

namespace SyncClient
{
    public static class Sync
    {
        public static void Synchronize()
        {
            ConnectionProperties
                remote = new ConnectionProperties(Settings.Default.RemoteServer, Settings.Default.RemoteDb),
                local = new ConnectionProperties(Settings.Default.LocalServer, Settings.Default.LocalDb);

            //check for schema update
            byte version;
            using (var proxy = new SchemaServiceClient())
                version = proxy.GetSchemaVersion();

            if (version > Settings.Default.SchemaVersion) {
                //synchronise schema
                StructureProject structure = new StructureProject(remote, local);

                structure.ComparisonOptions.IgnoreIdentitySeedAndIncrement = true;

                structure.MappedObjects.ExcludeAllFromComparison();
                structure.MappedTables.IncludeInComparison(Settings.Default.Tables.Split(',').Select(t => '^' + t + '$').ToArray());
                structure.ComparedObjects.IncludeAllInSynchronization();

                Synchronise(structure);

                Settings.Default.SchemaVersion = version;
                Settings.Default.Save();
            }

            //synchronise data
            DataProject data = new DataProject(remote, local);

            data.ComparedTables.IncludeAllInSynchronization();

            //receive server data
            Synchronise(data);

            //send local data
            data.SynchronizationOptions.SynchronizationDirection = SynchronizationDirection.DestinationToSource;
            Synchronise(data);

            //do we have an ID range?
            if (Settings.Default.MaxID == 0) {
                using (var client = new SchemaServiceClient()) {
                    var range = client.GetIdRange(Environment.MachineName);
                    Settings.Default.MinID = range.Min;
                    Settings.Default.MaxID = range.Max;
                    Settings.Default.Save();
                }
            }

            //reseed the client
            using (var db = new SqlConnection(Settings.Default.Local))
                Repository.Repository.Reseed(Settings.Default.MinID, Settings.Default.MaxID, db);
        }

        private static void Synchronise(ProjectBase project)
        {
            try {
                var errors = project.ExecuteSynchronizationScript();
                if (errors.Length > 0)
                    throw new ApplicationException(string.Join("\n", errors));
            }
            catch (UnhandledException e) {
                if (!(e.InnerException is NoSelectedForOperationObjectsException))
                    throw;
            }
        }

        public static bool IsProvisioned()
        {
            return Settings.Default.SchemaVersion > 0;
        }
    }
}