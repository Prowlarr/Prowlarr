import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import withScrollPosition from 'Components/withScrollPosition';
import { testAllIndexers } from 'Store/Actions/indexerActions';
import { saveIndexerEditor, setMovieFilter, setMovieSort, setMovieTableOption } from 'Store/Actions/indexerIndexActions';
import scrollPositions from 'Store/scrollPositions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createIndexerClientSideCollectionItemsSelector from 'Store/Selectors/createIndexerClientSideCollectionItemsSelector';
import IndexerIndex from './IndexerIndex';

function createMapStateToProps() {
  return createSelector(
    createIndexerClientSideCollectionItemsSelector('indexerIndex'),
    createDimensionsSelector(),
    (
      indexers,
      dimensionsState
    ) => {
      return {
        ...indexers,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setMovieTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setMovieSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setMovieFilter({ selectedFilterKey }));
    },

    dispatchSaveIndexerEditor(payload) {
      dispatch(saveIndexerEditor(payload));
    },

    onTestAllPress() {
      dispatch(testAllIndexers());
    }
  };
}

class IndexerIndexConnector extends Component {

  //
  // Listeners

  onSaveSelected = (payload) => {
    this.props.dispatchSaveIndexerEditor(payload);
  };

  onScroll = ({ scrollTop }) => {
    scrollPositions.movieIndex = scrollTop;
  };

  //
  // Render

  render() {
    return (
      <IndexerIndex
        {...this.props}
        onScroll={this.onScroll}
        onSaveSelected={this.onSaveSelected}
      />
    );
  }
}

IndexerIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  dispatchSaveIndexerEditor: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object)
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(IndexerIndexConnector),
  'indexerIndex'
);
