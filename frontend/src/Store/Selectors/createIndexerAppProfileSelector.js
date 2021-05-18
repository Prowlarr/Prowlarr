import { createSelector } from 'reselect';
import createIndexerSelector from './createIndexerSelector';

function createIndexerAppProfileSelector() {
  return createSelector(
    (state) => state.settings.appProfiles.items,
    createIndexerSelector(),
    (appProfiles, indexer = {}) => {
      return appProfiles.find((profile) => {
        return profile.id === indexer.appProfileId;
      });
    }
  );
}

export default createIndexerAppProfileSelector;
