using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(034)]
    public class history_fix_data_titles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateHistoryDataTitle);
        }

        private void MigrateHistoryDataTitle(IDbConnection conn, IDbTransaction tran)
        {
            var updatedHistory = new List<object>();

            using (var selectCommand = conn.CreateCommand())
            {
                selectCommand.Transaction = tran;
                selectCommand.CommandText = "SELECT \"Id\", \"Data\", \"EventType\" FROM \"History\" WHERE \"EventType\" != 3";

                using var reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var data = reader.GetString(1);
                    var eventType = reader.GetInt32(2);

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        var jsonObject = Json.Deserialize<JObject>(data);

                        if (eventType == 1 && jsonObject.ContainsKey("title"))
                        {
                            jsonObject.Add("grabTitle", jsonObject.Value<string>("title"));
                            jsonObject.Remove("title");
                        }

                        if (eventType != 1 && jsonObject.ContainsKey("bookTitle"))
                        {
                            jsonObject.Add("title", jsonObject.Value<string>("bookTitle"));
                            jsonObject.Remove("bookTitle");
                        }

                        data = jsonObject.ToJson();

                        if (!jsonObject.ContainsKey("grabTitle") && !jsonObject.ContainsKey("title"))
                        {
                            continue;
                        }

                        updatedHistory.Add(new
                        {
                            Id = id,
                            Data = data
                        });
                    }
                }
            }

            var updateHistorySql = "UPDATE \"History\" SET \"Data\" = @Data WHERE \"Id\" = @Id";
            conn.Execute(updateHistorySql, updatedHistory, transaction: tran);
        }
    }
}
