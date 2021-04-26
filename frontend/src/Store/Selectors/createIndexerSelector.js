import { createSelector } from 'reselect';

function createIndexerSelector() {
  return createSelector(
    (state, { indexerId }) => indexerId,
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (indexerId, itemMap, allIndexers) => {
      if (allIndexers && itemMap && indexerId in itemMap) {
        return allIndexers[itemMap[indexerId]];
      }
      return undefined;
    }
  );
}

export default createIndexerSelector;
