using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(031)]
    public class apprise_server_url : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateToServerUrl);
        }

        private void MigrateToServerUrl(IDbConnection conn, IDbTransaction tran)
        {
            using var selectCommand = conn.CreateCommand();
            selectCommand.Transaction = tran;
            selectCommand.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Notifications\" WHERE \"Implementation\" = 'Apprise'";

            using var reader = selectCommand.ExecuteReader();

            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var settings = reader.GetString(1);

                if (!string.IsNullOrWhiteSpace(settings))
                {
                    var jsonObject = Json.Deserialize<JObject>(settings);

                    if (jsonObject.ContainsKey("baseUrl"))
                    {
                        jsonObject.Add("serverUrl", jsonObject.Value<string>("baseUrl"));
                        jsonObject.Remove("baseUrl");
                    }

                    settings = jsonObject.ToJson();
                }

                var parameters = new { Settings = settings, Id = id };
                conn.Execute("UPDATE Notifications SET Settings = @Settings WHERE Id = @Id", parameters, transaction: tran);
            }
        }
    }
}
