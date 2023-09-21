using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(041)]
    public class gazelle_freeleech_token_options : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateIndexersToTokenOptions);
        }

        private void MigrateIndexersToTokenOptions(IDbConnection conn, IDbTransaction tran)
        {
            var updated = new List<object>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" IN ('Orpheus', 'Redacted', 'AlphaRatio', 'BrokenStones', 'CGPeers', 'DICMusic', 'GreatPosterWall', 'SecretCinema')";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = Json.Deserialize<JObject>(reader.GetString(1));

                        if (settings.ContainsKey("useFreeleechToken") && settings.Value<JToken>("useFreeleechToken").Type == JTokenType.Boolean)
                        {
                            var optionValue = settings.Value<bool>("useFreeleechToken") switch
                            {
                                true => 2, // Required
                                _ => 0 // Never
                            };

                            settings.Remove("useFreeleechToken");
                            settings.Add("useFreeleechToken", optionValue);
                        }

                        updated.Add(new
                        {
                            Id = id,
                            Settings = settings.ToJson()
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"Indexers\" SET \"Settings\" = @Settings WHERE \"Id\" = @Id";
            conn.Execute(updateSql, updated, transaction: tran);
        }
    }
}
