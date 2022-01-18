import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllIndexersSelector from './createAllIndexersSelector';

function createExistingIndexerSelector() {
  return createSelector(
    (state, { definitionName }) => definitionName,
    createAllIndexersSelector(),
    (definitionName, indexers) => {
      return _.some(indexers, { definitionName });
    }
  );
}

export default createExistingIndexerSelector;
