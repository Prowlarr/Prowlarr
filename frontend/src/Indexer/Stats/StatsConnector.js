import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchIndexerStats, setIndexerStatsFilter } from 'Store/Actions/indexerStatsActions';
import Stats from './Stats';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexerStats,
    (indexerStats) => indexerStats
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onFilterSelect(selectedFilterKey) {
      dispatch(setIndexerStatsFilter({ selectedFilterKey }));
    },
    dispatchFetchIndexerStats() {
      dispatch(fetchIndexerStats());
    }
  };
}

class StatsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchIndexerStats();
  }

  //
  // Render

  render() {
    return (
      <Stats
        {...this.props}
      />
    );
  }
}

StatsConnector.propTypes = {
  dispatchFetchIndexerStats: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, createMapDispatchToProps)(StatsConnector);
