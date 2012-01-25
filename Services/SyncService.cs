using System.Data.SqlClient;
using System.Linq;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

using Services.Properties;

namespace Services
{
    public class SyncService : ISyncService
    {
        SqlSyncProvider provider;

        public DbSyncScopeDescription GetScopeDescription(string scope)
        {
            using (var con = new SqlConnection(Settings.Default.Server))
                return SqlSyncDescriptionBuilder.GetDescriptionForScope(scope, con);
        }

        public void BeginSession(string scopeName, SyncIdFormatGroup idFormatGroup)
        {
            provider = new SqlSyncProvider(scopeName, new SqlConnection(Settings.Default.Server));
            provider.BeginSession(SyncProviderPosition.Remote, new SyncSessionContext(idFormatGroup, null));
        }

        public SyncIdFormatGroup GetIdFormats()
        {
            return provider == null ? null : provider.IdFormats;
        }

        public void GetKnowledge(out uint batchSize, out SyncKnowledge knowledge)
        {
            provider.GetSyncBatchParameters(out batchSize, out knowledge);
        }

        public ChangeBatch GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, out object changeData)
        {
            return provider.GetChangeBatch(batchSize, destinationKnowledge, out changeData);
        }

        public void ApplyChanges(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, object changeData, ref SyncSessionStatistics sessionStatistics)
        {
            provider.ProcessChangeBatch(resolutionPolicy, sourceChanges, changeData, new SyncCallbacks(), sessionStatistics);
        }

        public void EndSession()
        {
            provider.Dispose();
            provider = null;
            //reseed
            using (var con = new SqlConnection(Settings.Default.Server))
                Repository.Repository.Reseed(Settings.Default.MinId, Settings.Default.MaxId, con);
        }

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