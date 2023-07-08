import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('indexers', uiSection),
    (indexers) => {
      const items = indexers.items.map((s) => {
        const {
          id,
          sortName
        } = s;

        return {
          id,
          sortName
        };
      });

      return {
        ...indexers,
        items
      };
    }
  );
}

function movieListEqual(a, b) {
  return hasDifferentItemsOrOrder(a, b);
}

const createMovieEqualSelector = createSelectorCreator(
  defaultMemoize,
  movieListEqual
);

function createIndexerClientSideCollectionItemsSelector(uiSection) {
  return createMovieEqualSelector(
    createUnoptimizedSelector(uiSection),
    (indexers) => indexers
  );
}

export default createIndexerClientSideCollectionItemsSelector;
