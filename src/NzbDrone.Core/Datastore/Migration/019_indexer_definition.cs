using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(19)]
    public class indexer_definition : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("Indexers")
                .AddColumn("DefinitionFile").AsString().Nullable();

            Execute.WithConnection(MigrateCardigannDefinitions);
        }

        private void MigrateCardigannDefinitions(IDbConnection conn, IDbTransaction tran)
        {
            var indexers = new List<Indexer017>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Id\", \"Settings\", \"Implementation\", \"ConfigContract\" FROM \"Indexers\"";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var settings = reader.GetString(1);
                        var implementation = reader.GetString(2);
                        var configContract = reader.GetString(3);
                        var defFile = implementation.ToLowerInvariant();

                        if (implementation == "Cardigann")
                        {
                            if (!string.IsNullOrWhiteSpace(settings))
                            {
                                var jsonObject = STJson.Deserialize<JsonElement>(settings);

                                if (jsonObject.TryGetProperty("definitionFile", out JsonElement jsonDef))
                                {
                                    defFile = jsonDef.GetString();
                                }
                            }
                        }
                        else if (configContract == "AvistazSettings")
                        {
                            implementation = "Avistaz";
                        }
                        else if (configContract == "Unit3dSettings")
                        {
                            implementation = "Unit3d";
                        }
                        else if (configContract == "Newznab")
                        {
                            defFile = "";
                        }

                        indexers.Add(new Indexer017
                        {
                            DefinitionFile = defFile,
                            Implementation = implementation,
                            Id = id
                        });
                    }
                }
            }

            var updateSql = "UPDATE \"Indexers\" SET \"DefinitionFile\" = @DefinitionFile, \"Implementation\" = @Implementation WHERE \"Id\" = @Id";
            conn.Execute(updateSql, indexers, transaction: tran);
        }

        public class Indexer017
        {
            public int Id { get; set; }
            public string DefinitionFile { get; set; }
            public string Implementation { get; set; }
        }
    }
}
