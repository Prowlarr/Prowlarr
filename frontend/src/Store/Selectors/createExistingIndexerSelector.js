import { some } from 'lodash-es';
import { createSelector } from 'reselect';
import createAllIndexersSelector from './createAllIndexersSelector';

function createExistingIndexerSelector() {
  return createSelector(
    (state, { definitionName }) => definitionName,
    createAllIndexersSelector(),
    (definitionName, indexers) => {
      return some(indexers, { definitionName });
    }
  );
}

export default createExistingIndexerSelector;
