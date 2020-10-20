import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import withScrollPosition from 'Components/withScrollPosition';
import { fetchReleases, setReleasesFilter, setReleasesSort, setReleasesTableOption } from 'Store/Actions/releaseActions';
import scrollPositions from 'Store/scrollPositions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createReleaseClientSideCollectionItemsSelector from 'Store/Selectors/createReleaseClientSideCollectionItemsSelector';
import SearchIndex from './SearchIndex';

function createMapStateToProps() {
  return createSelector(
    createReleaseClientSideCollectionItemsSelector('releases'),
    createDimensionsSelector(),
    (
      movies,
      dimensionsState
    ) => {
      return {
        ...movies,
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
    }
  };
}

class SearchIndexConnector extends Component {

  onScroll = ({ scrollTop }) => {
    scrollPositions.movieIndex = scrollTop;
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
  items: PropTypes.arrayOf(PropTypes.object)
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(SearchIndexConnector),
  'releases'
);
