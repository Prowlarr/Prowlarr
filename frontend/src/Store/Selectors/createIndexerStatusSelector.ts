import { find } from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { IndexerStatus } from 'Indexer/Indexer';

function createIndexerStatusSelector(indexerId: number) {
  return createSelector(
    (state: AppState) => state.indexerStatus.items,
    (indexerStatus) => {
      return find(indexerStatus, { indexerId }) as IndexerStatus | undefined;
    }
  );
}

export default createIndexerStatusSelector;
