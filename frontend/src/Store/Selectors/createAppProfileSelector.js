import { createSelector } from 'reselect';

function createAppProfileSelector() {
  return createSelector(
    (state, { appProfileId }) => appProfileId,
    (state) => state.settings.appProfiles.items,
    (appProfileId, appProfiles) => {
      return appProfiles.find((profile) => {
        return profile.id === appProfileId;
      });
    }
  );
}

export default createAppProfileSelector;
