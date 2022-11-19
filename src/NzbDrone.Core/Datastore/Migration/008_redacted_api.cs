using System.Collections.Generic;
using System.Data;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(8)]
    public class redacted_api : NzbDroneMigrationBase
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
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" = 'Redacted'";

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
                                ConfigContract = "RedactedSettings",
                                Enable = false
                            });
                        }
                    }
                }
            }
        }

        public class Indexer008
        {
            public int Id { get; set; }
            public string Settings { get; set; }
            public string ConfigContract { get; set; }
            public bool Enable { get; set; }
        }
    }
}
