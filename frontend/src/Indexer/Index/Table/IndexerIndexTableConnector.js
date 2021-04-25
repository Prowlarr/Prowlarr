import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setMovieSort } from 'Store/Actions/indexerIndexActions';
import IndexerIndexTable from './IndexerIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    (state) => state.indexerIndex.tableOptions,
    (state) => state.indexerIndex.columns,
    (dimensions, tableOptions, columns) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        showBanners: tableOptions.showBanners,
        columns
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setMovieSort({ sortKey }));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IndexerIndexTable);
