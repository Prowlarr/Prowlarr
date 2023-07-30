import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Indexer from 'Indexer/Indexer';
import { createIndexerSelectorForHook } from './createIndexerSelector';

function createIndexerAppProfileSelector(indexerId: number) {
  return createSelector(
    (state: AppState) => state.settings.appProfiles.items,
    createIndexerSelectorForHook(indexerId),
    (appProfiles, indexer = {} as Indexer) => {
      return appProfiles.find((profile) => profile.id === indexer.appProfileId);
    }
  );
}

export default createIndexerAppProfileSelector;
