import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import IndexerIndexTableOptions from './IndexerIndexTableOptions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexerIndex.tableOptions,
    (tableOptions) => {
      return tableOptions;
    }
  );
}

export default connect(createMapStateToProps)(IndexerIndexTableOptions);
