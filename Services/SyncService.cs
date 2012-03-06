using System.Data.SqlClient;
using System.Linq;
using System.ServiceModel;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

using Services.Properties;

namespace Services
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    [ServiceKnownType(typeof(DbSyncContext))]
    [ServiceKnownType(typeof(SyncIdFormatGroup))]
    public class SyncService
    {
        SqlSyncProvider provider;

        [OperationContract()]
        public DbSyncScopeDescription GetScopeDescription(string scope)
        {
            using (var con = new SqlConnection(Settings.Default.Server))
                return SqlSyncDescriptionBuilder.GetDescriptionForScope(scope, con);
        }

        [OperationContract()]
        public void BeginSession(string scopeName, SyncIdFormatGroup idFormatGroup)
        {
            provider = new SqlSyncProvider(scopeName, new SqlConnection(Settings.Default.Server));
            provider.BeginSession(SyncProviderPosition.Remote, new SyncSessionContext(idFormatGroup, null));
        }

        [OperationContract()]
        public SyncIdFormatGroup GetIdFormats()
        {
            return provider == null ? null : provider.IdFormats;
        }

        [OperationContract()]
        public void GetKnowledge(out uint batchSize, out SyncKnowledge knowledge)
        {
            provider.GetSyncBatchParameters(out batchSize, out knowledge);
        }

        [OperationContract()]
        public ChangeBatch GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, out object changeData)
        {
            return provider.GetChangeBatch(batchSize, destinationKnowledge, out changeData);
        }

        [OperationContract()]
        public void ApplyChanges(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, object changeData, ref SyncSessionStatistics sessionStatistics)
        {
            provider.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeData, new SyncCallbacks(), sessionStatistics);
        }

        [OperationContract()]
        public void EndSession()
        {
            provider.Dispose();
            provider = null;
            //reseed
            using (var con = new SqlConnection(Settings.Default.Server))
                Repository.Repository.Reseed(Settings.Default.MinId, Settings.Default.MaxId, con);
        }

        [OperationContract()]
        public Range GetIdRange(string machine)
        {
            IdRange range;
            using (var context = new IdRangesDataContext()) {
                if ((range = context.IdRanges.SingleOrDefault(r => r.Machine == machine)) == null) {
                    range = new IdRange() { Machine = machine };
                    range.Min = context.IdRanges.Max(r => r.Max) + 1;
                    range.Max = range.Min + Settings.Default.IdRange - 1;
                    context.IdRanges.InsertOnSubmit(range);
                    context.SubmitChanges();
                }
                return new Range(range.Min, range.Max);
            }
        }
    }
}