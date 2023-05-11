using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;
using static NzbDrone.Core.Datastore.Migration.redacted_api;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(22)]
    public class orpheus_api : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateToRedactedApi);
        }

        private void MigrateToRedactedApi(IDbConnection conn, IDbTransaction tran)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" = 'Orpheus'";

                var updatedIndexers = new List<Indexer008>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = reader.GetString(1);
                        if (!string.IsNullOrWhiteSpace(settings))
                        {
                            var jsonObject = Json.Deserialize<JObject>(settings);

                            // Remove username
                            if (jsonObject.ContainsKey("username"))
                            {
                                jsonObject.Remove("username");
                            }

                            // Remove password
                            if (jsonObject.ContainsKey("password"))
                            {
                                jsonObject.Remove("password");
                            }

                            // write new json back to db, switch to new ConfigContract, and disable the indexer
                            settings = jsonObject.ToJson();

                            updatedIndexers.Add(new Indexer008
                            {
                                Id = id,
                                Settings = settings,
                                ConfigContract = "OrpheusSettings",
                                Enable = false
                            });
                        }
                    }
                }

                var updateSql = "UPDATE \"Indexers\" SET \"Settings\" = @Settings, \"ConfigContract\" = @ConfigContract, \"Enable\" = @Enable WHERE \"Id\" = @Id";
                conn.Execute(updateSql, updatedIndexers, transaction: tran);
            }
        }
    }
}
