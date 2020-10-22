import { createSelector } from 'reselect';

function createAllIndexersSelector() {
  return createSelector(
    (state) => state.indexers,
    (indexers) => {
      return indexers.items;
    }
  );
}

export default createAllIndexersSelector;
