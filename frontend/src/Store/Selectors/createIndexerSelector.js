import { createSelector } from 'reselect';

function createIndexerSelector(id) {
  if (id == null) {
    return createSelector(
      (state, { indexerId }) => indexerId,
      (state) => state.indexers.itemMap,
      (state) => state.indexers.items,
      (indexerId, itemMap, allIndexers) => {
        return allIndexers[itemMap[indexerId]];
      }
    );
  }

  return createSelector(
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (itemMap, allIndexers) => {
      return allIndexers[itemMap[id]];
    }
  );
}

export default createIndexerSelector;
