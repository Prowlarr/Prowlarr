using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(042)]
    public class myanonamouse_freeleech_wedge_options : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateIndexersToWedgeOptions);
        }

        private void MigrateIndexersToWedgeOptions(IDbConnection conn, IDbTransaction tran)
        {
            var updated = new List<object>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" = 'MyAnonamouse'";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = Json.Deserialize<JObject>(reader.GetString(1));

                        if (settings.ContainsKey("freeleech") && settings.Value<JToken>("freeleech").Type == JTokenType.Boolean)
                        {
                            var optionValue = settings.Value<bool>("freeleech") switch
                            {
                                true => 2, // Required
                                _ => 0 // Never
                            };

                            settings.Remove("freeleech");
                            settings.Add("useFreeleechWedge", optionValue);
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
