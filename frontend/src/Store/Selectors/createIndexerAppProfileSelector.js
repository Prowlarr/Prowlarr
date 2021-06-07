import { createSelector } from 'reselect';
import createIndexerSelector from './createIndexerSelector';

function createIndexerAppProfileSelector() {
  return createSelector(
    (state) => state.settings.appProfiles.items,
    createIndexerSelector(),
    (appProfiles, indexer = {}) => {
      return appProfiles.filter((profile) => indexer.appProfileIds.includes(profile.id));
    }
  );
}

export default createIndexerAppProfileSelector;
