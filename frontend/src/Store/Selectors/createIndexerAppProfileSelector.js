import { createSelector } from 'reselect';
import createIndexerSelector from './createIndexerSelector';

function createIndexerAppProfileSelector(indexerId) {
  return createSelector(
    (state) => state.settings.appProfiles.items,
    createIndexerSelector(indexerId),
    (appProfiles, indexer = {}) => {
      return appProfiles.find((profile) => {
        return profile.id === indexer.appProfileId;
      });
    }
  );
}

export default createIndexerAppProfileSelector;
