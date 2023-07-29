import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { createIndexerSelectorForHook } from './createIndexerSelector';

function createIndexerAppProfileSelector(indexerId: number) {
  return createSelector(
    (state: AppState) => state.settings.appProfiles.items,
    createIndexerSelectorForHook(indexerId),
    (appProfiles, indexer = {}) => {
      return appProfiles.find((profile) => {
        return profile.id === indexer.appProfileId;
      });
    }
  );
}

export default createIndexerAppProfileSelector;
