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

            using (var service = new SchemaServiceClient()) {

                //check for schema update
                byte version = service.GetSchemaVersion();
                if (version > Settings.Default.SchemaVersion) {
                    //synchronise schema
                    StructureProject project = new StructureProject(remote, local);

                    project.ComparisonOptions.IgnoreIdentitySeedAndIncrement = true;

                    project.MappedObjects.ExcludeAllFromComparison();
                    project.MappedTables.IncludeInComparison(Settings.Default.Tables.Split(',').Select(t => '^' + t + '$').ToArray());
                    project.ComparedObjects.IncludeAllInSynchronization();

                    Synchronise(project);

                    Settings.Default.SchemaVersion = version;
                    Settings.Default.Save();
                }

                //synchronise data
                SynchroniseData(remote, local);
                SynchroniseData(local, remote);

                //do we have an ID range?
                if (Settings.Default.MaxID == 0) {
                    var range = service.GetIdRange(Environment.MachineName);
                    Settings.Default.MinID = range.Min;
                    Settings.Default.MaxID = range.Max;
                    Settings.Default.Save();
                }
            }

            //reseed the client
            using (var db = new SqlConnection(Settings.Default.Local))
                Repository.Repository.Reseed(Settings.Default.MinID, Settings.Default.MaxID, db);
        }

        private static void SynchroniseData(ConnectionProperties source, ConnectionProperties destination)
        {
            DataProject project = new DataProject(source, destination);
#pragma warning disable 612
            project.ComparisonOptions.CompareAdditionalRows = false;
#pragma warning restore 612
            project.ComparedObjects.IncludeAllInSynchronization();
            Synchronise(project);
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
            finally {
                project.CloseConnectionToDataSources();
            }
        }

        public static bool IsProvisioned()
        {
            return Settings.Default.SchemaVersion > 0;
        }
    }
}