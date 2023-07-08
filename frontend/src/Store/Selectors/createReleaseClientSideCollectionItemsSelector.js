import { createSelector, createSelectorCreator, defaultMemoize } from 'reselect';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import createClientSideCollectionSelector from './createClientSideCollectionSelector';

function createUnoptimizedSelector(uiSection) {
  return createSelector(
    createClientSideCollectionSelector('releases', uiSection),
    (releases) => {
      const items = releases.items.map((s) => {
        const {
          guid,
          sortTitle,
          indexerId
        } = s;

        return {
          guid,
          sortTitle,
          indexerId
        };
      });

      return {
        ...releases,
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

function createReleaseClientSideCollectionItemsSelector(uiSection) {
  return createMovieEqualSelector(
    createUnoptimizedSelector(uiSection),
    (releases) => releases
  );
}

export default createReleaseClientSideCollectionItemsSelector;
