import { createSelector } from 'reselect';
import { createIndexerSelectorForHook } from './createIndexerSelector';

function createIndexerAppProfileSelector(indexerId) {
  return createSelector(
    (state) => state.settings.appProfiles.items,
    createIndexerSelectorForHook(indexerId),
    (appProfiles, indexer = {}) => {
      return appProfiles.find((profile) => {
        return profile.id === indexer.appProfileId;
      });
    }
  );
}

export default createIndexerAppProfileSelector;
