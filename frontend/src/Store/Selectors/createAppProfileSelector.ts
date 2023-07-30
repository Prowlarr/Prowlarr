import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createAppProfileSelector() {
  return createSelector(
    (_: AppState, { appProfileId }: { appProfileId: number }) => appProfileId,
    (state: AppState) => state.settings.appProfiles.items,
    (appProfileId, appProfiles) => {
      return appProfiles.find((profile) => profile.id === appProfileId);
    }
  );
}

export default createAppProfileSelector;
