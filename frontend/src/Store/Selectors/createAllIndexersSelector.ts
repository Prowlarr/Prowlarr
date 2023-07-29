import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createAllIndexersSelector() {
  return createSelector(
    (state: AppState) => state.indexers,
    (indexers) => {
      return indexers.items;
    }
  );
}

export default createAllIndexersSelector;
