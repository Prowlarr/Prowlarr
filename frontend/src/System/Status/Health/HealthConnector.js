import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { testAllIndexers } from 'Store/Actions/indexerActions';
import { testAllApplications } from 'Store/Actions/Settings/applications';
import { testAllDownloadClients } from 'Store/Actions/Settings/downloadClients';
import { fetchHealth } from 'Store/Actions/systemActions';
import createHealthCheckSelector from 'Store/Selectors/createHealthCheckSelector';
import Health from './Health';

function createMapStateToProps() {
  return createSelector(
    createHealthCheckSelector(),
    (state) => state.system.health,
    (state) => state.settings.applications.isTestingAll,
    (state) => state.settings.downloadClients.isTestingAll,
    (state) => state.indexers.isTestingAll,
    (items, health, isTestingAllApplications, isTestingAllDownloadClients, isTestingAllIndexers) => {
      const {
        isFetching,
        isPopulated
      } = health;

      return {
        isFetching,
        isPopulated,
        items,
        isTestingAllApplications,
        isTestingAllDownloadClients,
        isTestingAllIndexers
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchHealth: fetchHealth,
  dispatchTestAllApplications: testAllApplications,
  dispatchTestAllDownloadClients: testAllDownloadClients,
  dispatchTestAllIndexers: testAllIndexers
};

class HealthConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchHealth();
  }

  //
  // Render

  render() {
    const {
      dispatchFetchHealth,
      ...otherProps
    } = this.props;

    return (
      <Health
        {...otherProps}
      />
    );
  }
}

HealthConnector.propTypes = {
  dispatchFetchHealth: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(HealthConnector);
