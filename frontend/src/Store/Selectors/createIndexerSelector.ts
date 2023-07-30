import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

export function createIndexerSelectorForHook(indexerId: number) {
  return createSelector(
    (state: AppState) => state.indexers.itemMap,
    (state: AppState) => state.indexers.items,
    (itemMap, allIndexers) => {
      return indexerId ? allIndexers[itemMap[indexerId]] : undefined;
    }
  );
}

function createIndexerSelector() {
  return createSelector(
    (_: AppState, { indexerId }: { indexerId: number }) => indexerId,
    (state: AppState) => state.indexers.itemMap,
    (state: AppState) => state.indexers.items,
    (indexerId, itemMap, allIndexers) => {
      return allIndexers[itemMap[indexerId]];
    }
  );
}

export default createIndexerSelector;
