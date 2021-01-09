import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchIndexerStats } from 'Store/Actions/indexerStatsActions';
import Stats from './Stats';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexerStats,
    (indexerStats) => indexerStats
  );
}

const mapDispatchToProps = {
  dispatchFetchIndexers: fetchIndexerStats
};

class StatsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchIndexers();
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
  dispatchFetchIndexers: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(StatsConnector);
