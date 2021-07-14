import { createSelector } from 'reselect';

function createAppProfileSelector() {
  return createSelector(
    (state, { appProfileIds }) => appProfileIds,
    (state) => state.settings.appProfiles.items,
    (appProfileIds, appProfiles) => {
      return appProfiles.find((profile) => {
        return profile.id === appProfileIds;
      });
    }
  );
}

export default createAppProfileSelector;
