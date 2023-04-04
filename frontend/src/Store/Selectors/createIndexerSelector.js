import { createSelector } from 'reselect';

export function createIndexerSelectorForHook(indexerId) {
  return createSelector(
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (itemMap, allIndexers) => {
      return indexerId ? allIndexers[itemMap[indexerId]]: undefined;
    }
  );
}

function createIndexerSelector() {
  return createSelector(
    (state, { indexerId }) => indexerId,
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (indexerId, itemMap, allIndexers) => {
      return allIndexers[itemMap[indexerId]];
    }
  );
}

export default createIndexerSelector;
