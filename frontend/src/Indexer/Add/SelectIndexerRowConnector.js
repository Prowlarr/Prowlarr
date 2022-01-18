
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createExistingIndexerSelector from 'Store/Selectors/createExistingIndexerSelector';
import SelectIndexerRow from './SelectIndexerRow';

function createMapStateToProps() {
  return createSelector(
    createExistingIndexerSelector(),
    (isExistingIndexer, dimensions) => {
      return {
        isExistingIndexer
      };
    }
  );
}

export default connect(createMapStateToProps)(SelectIndexerRow);
