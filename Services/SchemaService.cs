using System.Linq;
using System.ServiceModel;
using System.Data.SqlClient;

using Services.Properties;

namespace Services
{
    [ServiceContract]
    public class SchemaService
    {
        [OperationContract]
        public byte GetSchemaVersion()
        {
            return Settings.Default.SchemaVersion;
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

        [OperationContract]
        public void ReSeed()
        {
            using (var db = new SqlConnection(Settings.Default.Server))
                Repository.Repository.Reseed(Settings.Default.MinId, Settings.Default.MaxId, db);
        }
    }
}