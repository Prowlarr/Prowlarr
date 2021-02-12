import _ from 'lodash';
import { createSelector } from 'reselect';

function createIndexerStatusSelector() {
  return createSelector(
    (state, { indexerId }) => indexerId,
    (state) => state.indexerStatus.items,
    (indexerId, indexerStatus) => {
      return _.find(indexerStatus, { indexerId });
    }
  );
}

export default createIndexerStatusSelector;
