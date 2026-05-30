using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(044)]
    public class myanonamouse_freeleech_wedge_checkbox : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateIndexersToWedgeCheckbox);
        }

        private void MigrateIndexersToWedgeCheckbox(IDbConnection conn, IDbTransaction tran)
        {
            var updated = new List<object>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" = 'MyAnonamouse'";

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var settings = Json.Deserialize<JObject>(reader.GetString(1));

                    if (settings.ContainsKey("useFreeleechWedge") && settings.Value<JToken>("useFreeleechWedge").Type == JTokenType.Integer)
                    {
                        var optionValue = settings.Value<int>("useFreeleechWedge") switch
                        {
                            1 or 2 => true, // Preferred / Required -> Enabled
                            _ => false, // Never -> Disabled
                        };

                        settings.Remove("useFreeleechWedge");
                        settings.Add("useFreeleechWedge", optionValue);
                    }

                    updated.Add(new
                    {
                        Id = id,
                        Settings = settings.ToJson()
                    });
                }
            }

            const string updateSql = "UPDATE \"Indexers\" SET \"Settings\" = @Settings WHERE \"Id\" = @Id";
            conn.Execute(updateSql, updated, transaction: tran);
        }
    }
}
