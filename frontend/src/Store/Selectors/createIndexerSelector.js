import { createSelector } from 'reselect';

function createIndexerSelector() {
  return createSelector(
    (state, { indexerId }) => indexerId,
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (indexerId, itemMap, allMovies) => {
      if (allMovies && itemMap && indexerId in itemMap) {
        return allMovies[itemMap[indexerId]];
      }
      return undefined;
    }
  );
}

export default createIndexerSelector;
