using System;
using LocalData.SyncService;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;

namespace LocalData
{
    class SyncProxy : KnowledgeSyncProvider, IDisposable
    {
        string scope;
        ISyncService proxy;
        SyncIdFormatGroup idFormatGroup;

        public SyncProxy(string scope)
        {
            this.scope = scope;
            proxy = new SyncServiceClient();
        }

        public override SyncIdFormatGroup IdFormats
        {
            get
            {
                if (idFormatGroup == null) {
                    idFormatGroup = new SyncIdFormatGroup();

                    //
                    // 1 byte change unit ID
                    //
                    idFormatGroup.ChangeUnitIdFormat.IsVariableLength = false;
                    idFormatGroup.ChangeUnitIdFormat.Length = 1;

                    //
                    // GUID replica ID
                    //
                    idFormatGroup.ReplicaIdFormat.IsVariableLength = false;
                    idFormatGroup.ReplicaIdFormat.Length = 16;


                    //
                    // Global ID for item IDs
                    //
                    idFormatGroup.ItemIdFormat.IsVariableLength = true;
                    idFormatGroup.ItemIdFormat.Length = 10 * 1024;
                }

                return idFormatGroup;
            }
        }
    

        public override void BeginSession(SyncProviderPosition position, SyncSessionContext syncSessionContext)
        {
            proxy.BeginSession(scope, idFormatGroup);
        }

        public override void EndSession(SyncSessionContext syncSessionContext)
        {
            proxy.EndSession();
        }

        public override void GetSyncBatchParameters(out uint batchSize, out SyncKnowledge knowledge)
        {
            batchSize = proxy.GetKnowledge(out knowledge);
        }

        public override ChangeBatch GetChangeBatch(uint batchSize, SyncKnowledge destinationKnowledge, out object changeDataRetriever)
        {
            return proxy.GetChanges(out changeDataRetriever, batchSize, destinationKnowledge);
        }

        public override void ProcessChangeBatch(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallback, SyncSessionStatistics sessionStatistics)
        {
            proxy.ApplyChanges(resolutionPolicy, sourceChanges, changeDataRetriever, ref sessionStatistics);
        }

        public override FullEnumerationChangeBatch GetFullEnumerationChangeBatch(uint batchSize, SyncId lowerEnumerationBound, SyncKnowledge knowledgeForDataRetrieval, out object changeDataRetriever)
        {
            throw new NotImplementedException();
        }

        public override void ProcessFullEnumerationChangeBatch(ConflictResolutionPolicy resolutionPolicy, FullEnumerationChangeBatch sourceChanges, object changeDataRetriever, SyncCallbacks syncCallback,	SyncSessionStatistics sessionStatistics)
        {
            throw new NotImplementedException();
        }

        public DbSyncScopeDescription GetScopeDescription()
        {
            return proxy.GetScopeDescription(scope);
        }

        public void Dispose()
        {
            if (proxy == null) return;
            ((IDisposable) proxy).Dispose();
            proxy = null;
        }
    }
}