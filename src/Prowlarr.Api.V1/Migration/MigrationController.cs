using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Migration;
using Prowlarr.Http;

namespace Prowlarr.Api.V1.Migration
{
    [V1ApiController]
    public class MigrationController : Controller
    {
        private readonly IJackettMigrationService _jackettMigrationService;

        public MigrationController(IJackettMigrationService jackettMigrationService)
        {
            _jackettMigrationService = jackettMigrationService;
        }

        //TODO: Make this work with the frontend (e.g. passing params for jackettPath and jackettApi
        [HttpPost("jackettmigration")]
        public void JackettMigration()
        {
            var jackettIndexers = _jackettMigrationService.GetJackettIndexers(jackettPath, jackettApi);

            foreach (var jackettIndexer in jackettIndexers)
            {
                var indexerconfig = _jackettMigrationService.GetJackettIndexerConfig(jackettIndexer, jackettPath, jackettApi);
                _jackettMigrationService.MigrateJackettIndexer(jackettIndexer, indexerconfig);
            }
        }
    }
}
