import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { executeCommand } from 'Store/Actions/commandActions';
import { testAllIndexers } from 'Store/Actions/indexerActions';
import { saveIndexerEditor, setMovieFilter, setMovieSort, setMovieTableOption } from 'Store/Actions/indexerIndexActions';
import scrollPositions from 'Store/scrollPositions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createIndexerClientSideCollectionItemsSelector from 'Store/Selectors/createIndexerClientSideCollectionItemsSelector';
import IndexerIndex from './IndexerIndex';

function createMapStateToProps() {
  return createSelector(
    createIndexerClientSideCollectionItemsSelector('indexerIndex'),
    createCommandExecutingSelector(commandNames.APP_INDEXER_SYNC),
    createDimensionsSelector(),
    (
      indexers,
      isSyncingIndexers,
      dimensionsState
    ) => {
      return {
        ...indexers,
        isSyncingIndexers,
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
    },

    onAppIndexerSyncPress() {
      dispatch(executeCommand({
        name: commandNames.APP_INDEXER_SYNC
      }));
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
