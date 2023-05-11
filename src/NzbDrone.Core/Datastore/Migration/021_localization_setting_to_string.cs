using System.Collections.Generic;
using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(021)]
    public class localization_setting_to_string : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(FixLocalizationConfig);
        }

        private void FixLocalizationConfig(IDbConnection conn, IDbTransaction tran)
        {
            string uiLanguage;
            string uiCulture;

            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'uilanguage'";

                uiLanguage = (string)cmd.ExecuteScalar();
            }

            if (uiLanguage != null && int.TryParse(uiLanguage, out var uiLanguageInt))
            {
                uiCulture = _uiMapping.GetValueOrDefault(uiLanguageInt) ?? "en";

                using (var insertCmd = conn.CreateCommand())
                {
                    insertCmd.Transaction = tran;
                    insertCmd.CommandText = string.Format("UPDATE \"Config\" SET \"Value\" = '{0}' WHERE \"Key\" = 'uilanguage'", uiCulture);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        private readonly Dictionary<int, string> _uiMapping = new Dictionary<int, string>()
        {
            { 1, "en" },
            { 2, "fr" },
            { 3, "es" },
            { 4, "de" },
            { 5, "it" },
            { 6, "da" },
            { 7, "nl" },
            { 8, "ja" },
            { 9, "is" },
            { 10, "zh_CN" },
            { 11, "ru" },
            { 12, "pl" },
            { 13, "vi" },
            { 14, "sv" },
            { 15, "nb_NO" },
            { 16, "fi" },
            { 17, "tr" },
            { 18, "pt" },
            { 19, "en" },
            { 20, "el" },
            { 21, "ko" },
            { 22, "hu" },
            { 23, "he" },
            { 24, "lt" },
            { 25, "cs" },
            { 26, "hi" },
            { 27, "ro" },
            { 28, "th" },
            { 29, "bg" },
            { 30, "pt_BR" },
            { 31, "ar" },
            { 32, "uk" },
            { 33, "fa" },
            { 34, "be" },
            { 35, "zh_TW" },
        };
    }
}
