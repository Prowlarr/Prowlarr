import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import withScrollPosition from 'Components/withScrollPosition';
import { bulkGrabReleases, cancelFetchReleases, clearReleases, fetchReleases, setReleasesFilter, setReleasesSort, setReleasesTableOption } from 'Store/Actions/releaseActions';
import { fetchDownloadClients } from 'Store/Actions/Settings/downloadClients';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createReleaseClientSideCollectionItemsSelector from 'Store/Selectors/createReleaseClientSideCollectionItemsSelector';
import SearchIndex from './SearchIndex';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexers,
    createReleaseClientSideCollectionItemsSelector('releases'),
    createDimensionsSelector(),
    (
      indexers,
      releases,
      dimensionsState
    ) => {
      return {
        ...releases,
        hasIndexers: indexers.items.length > 0,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setReleasesTableOption(payload));
    },

    onSortSelect(sortKey) {
      dispatch(setReleasesSort({ sortKey }));
    },

    onFilterSelect(selectedFilterKey) {
      dispatch(setReleasesFilter({ selectedFilterKey }));
    },

    onSearchPress(payload) {
      dispatch(fetchReleases(payload));
    },

    onBulkGrabPress(payload) {
      dispatch(bulkGrabReleases(payload));
    },

    dispatchCancelFetchReleases() {
      dispatch(cancelFetchReleases());
    },

    dispatchClearReleases() {
      dispatch(clearReleases());
    },

    dispatchFetchDownloadClients() {
      dispatch(fetchDownloadClients());
    }
  };
}

class SearchIndexConnector extends Component {

  componentDidMount() {
    this.props.dispatchFetchDownloadClients();
  }

  componentWillUnmount() {
    this.props.dispatchCancelFetchReleases();
    this.props.dispatchClearReleases();
  }

  //
  // Render

  render() {
    return (
      <SearchIndex
        {...this.props}
        onScroll={this.onScroll}
      />
    );
  }
}

SearchIndexConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  onBulkGrabPress: PropTypes.func.isRequired,
  dispatchCancelFetchReleases: PropTypes.func.isRequired,
  dispatchClearReleases: PropTypes.func.isRequired,
  dispatchFetchDownloadClients: PropTypes.func.isRequired,
  items: PropTypes.arrayOf(PropTypes.object)
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(SearchIndexConnector),
  'releases'
);
