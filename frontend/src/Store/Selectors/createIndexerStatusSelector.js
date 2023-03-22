import { find } from 'lodash-es';
import { createSelector } from 'reselect';

function createIndexerStatusSelector(indexerId) {
  return createSelector(
    (state) => state.indexerStatus.items,
    (indexerStatus) => {
      return find(indexerStatus, { indexerId });
    }
  );
}

export default createIndexerStatusSelector;
