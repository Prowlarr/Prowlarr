import { some } from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllIndexersSelector from './createAllIndexersSelector';

function createExistingIndexerSelector() {
  return createSelector(
    (_: AppState, { definitionName }: { definitionName: string }) =>
      definitionName,
    createAllIndexersSelector(),
    (definitionName, indexers) => {
      return some(indexers, { definitionName });
    }
  );
}

export default createExistingIndexerSelector;
