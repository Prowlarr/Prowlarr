import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { fetchIndexerConfig, saveIndexerConfig, setIndexerConfigValue } from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import IndexerSettings from './IndexerSettings';

const SECTION = 'indexerConfig';

function createMapStateToProps() {
  return createSelector(
    (state) => state.system.status.item.userAgent,
    createSettingsSectionSelector(SECTION),
    (defaultUserAgent, sectionSettings) => {
      return {
        defaultUserAgent,
        ...sectionSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setIndexerConfigValue,
  saveIndexerConfig,
  fetchIndexerConfig,
  clearPendingChanges
};

class IndexerSettingsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchIndexerConfig();
  }

  componentWillUnmount() {
    this.props.clearPendingChanges({ section: `settings.${SECTION}` });
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setIndexerConfigValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveIndexerConfig();
  };

  //
  // Render

  render() {
    return (
      <IndexerSettings
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
        {...this.props}
      />
    );
  }
}

IndexerSettingsConnector.propTypes = {
  setIndexerConfigValue: PropTypes.func.isRequired,
  saveIndexerConfig: PropTypes.func.isRequired,
  fetchIndexerConfig: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IndexerSettingsConnector);
