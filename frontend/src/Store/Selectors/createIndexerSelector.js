import { createSelector } from 'reselect';

function createIndexerSelector() {
  return createSelector(
    (state, { movieId }) => movieId,
    (state) => state.indexers.itemMap,
    (state) => state.indexers.items,
    (movieId, itemMap, allMovies) => {
      if (allMovies && itemMap && movieId in itemMap) {
        return allMovies[itemMap[movieId]];
      }
      return undefined;
    }
  );
}

export default createIndexerSelector;
