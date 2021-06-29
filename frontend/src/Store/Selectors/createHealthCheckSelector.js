import { createSelector } from 'reselect';
import translate from 'Utilities/String/translate';

function createHealthCheckSelector() {
  return createSelector(
    (state) => state.system.health,
    (state) => state.app,
    (health, app) => {
      const items = [...health.items];

      if (!app.isConnected) {
        items.push({
          source: 'UI',
          type: 'warning',
          message: translate('CouldNotConnectSignalR'),
          wikiUrl: 'https://wiki.servarr.com/prowlarr/system#could-not-connect-to-signalr'
        });
      }

      return items;
    }
  );
}

export default createHealthCheckSelector;
