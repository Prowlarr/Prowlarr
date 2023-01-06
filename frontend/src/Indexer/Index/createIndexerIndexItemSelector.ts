import { createSelector } from 'reselect';
import Indexer from 'Indexer/Indexer';
import createIndexerAppProfileSelector from 'Store/Selectors/createIndexerAppProfileSelector';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';

function createIndexerIndexItemSelector(indexerId: number) {
  return createSelector(
    createIndexerSelector(indexerId),
    createIndexerAppProfileSelector(indexerId),
    (indexer: Indexer, appProfile) => {
      // If a series is deleted this selector may fire before the parent
      // selectors, which will result in an undefined series, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a series that has no information available.

      if (!indexer) {
        return {};
      }

      return {
        indexer,
        appProfile,
      };
    }
  );
}

export default createIndexerIndexItemSelector;
