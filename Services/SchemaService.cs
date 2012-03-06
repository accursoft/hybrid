using System.ServiceModel;
using System.IO;

using ApexSql.Diff;
using ApexSql.Diff.Structure;

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

        [OperationContract]
        public string GetSchema()
        {
            string temp = Path.GetTempFileName();
            Diff.GenerateSnapshot(new ConnectionProperties(Settings.Default.ApexSqlServer, Settings.Default.ApexSqlDb), temp);
            string schema = File.ReadAllText(temp);
            File.Delete(temp);
            return schema;
        }
    }
}