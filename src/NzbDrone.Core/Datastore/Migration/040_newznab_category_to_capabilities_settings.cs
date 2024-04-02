using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(40)]
    public class newznab_category_to_capabilities_settings : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MoveCategoriesToCapabilities);
        }

        private void MoveCategoriesToCapabilities(IDbConnection conn, IDbTransaction tran)
        {
            var updated = new List<object>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\" FROM \"Indexers\" WHERE \"Implementation\" IN ('Newznab', 'Torznab')";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = Json.Deserialize<JObject>(reader.GetString(1));

                        if ((settings.Value<JObject>("capabilities")?.ContainsKey("categories") ?? false) == false
                            && settings.ContainsKey("categories")
                            && settings.TryGetValue("categories", out var categories))
                        {
                            if (!settings.ContainsKey("capabilities"))
                            {
                                settings.Add("capabilities", new JObject());
                            }

                            settings.Value<JObject>("capabilities")?.Add(new JProperty("categories", JArray.FromObject(categories)));

                            if (settings.ContainsKey("categories"))
                            {
                                settings.Remove("categories");
                            }
                        }

                        updated.Add(new
                        {
                            Settings = settings.ToJson(),
                            Id = id
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"Indexers\" SET \"Settings\" = @Settings WHERE \"Id\" = @Id";
            conn.Execute(updateSql, updated, transaction: tran);
        }
    }
}
