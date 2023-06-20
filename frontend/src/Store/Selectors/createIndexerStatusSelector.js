import _ from 'lodash';
import { createSelector } from 'reselect';

function createIndexerStatusSelector(indexerId) {
  return createSelector(
    (state) => state.indexerStatus.items,
    (indexerStatus) => {
      return _.find(indexerStatus, { indexerId });
    }
  );
}

export default createIndexerStatusSelector;
