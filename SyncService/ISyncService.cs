using System.ServiceModel;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;

namespace SyncService
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    [ServiceKnownType(typeof(DbSyncContext))]
    [ServiceKnownType(typeof(SyncIdFormatGroup))]
    public interface ISyncService
    {
        [OperationContract()]
        DbSyncScopeDescription GetScopeDescription(string scope);

        [OperationContract()]
        void BeginSession(string scopeName, SyncIdFormatGroup idFormatGroup);

        [OperationContract]
        SyncIdFormatGroup GetIdFormats();

        [OperationContract]
        void GetKnowledge(out uint batchSize, out SyncKnowledge knowledge);

        [OperationContract]
        ChangeBatch GetChanges(uint batchSize, SyncKnowledge destinationKnowledge, out object changeData);

        [OperationContract]
        void ApplyChanges(ConflictResolutionPolicy resolutionPolicy, ChangeBatch sourceChanges, object changeData, ref SyncSessionStatistics sessionStatistics);

        [OperationContract]
        void EndSession();
    }
}