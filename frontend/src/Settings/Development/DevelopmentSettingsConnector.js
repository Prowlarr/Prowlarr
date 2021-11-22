import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { fetchDevelopmentSettings, saveDevelopmentSettings, setDevelopmentSettingsValue } from 'Store/Actions/settingsActions';
import createSettingsSectionSelector from 'Store/Selectors/createSettingsSectionSelector';
import DevelopmentSettings from './DevelopmentSettings';

const SECTION = 'development';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createSettingsSectionSelector(SECTION),
    (advancedSettings, sectionSettings) => {
      return {
        advancedSettings,
        ...sectionSettings
      };
    }
  );
}

const mapDispatchToProps = {
  setDevelopmentSettingsValue,
  saveDevelopmentSettings,
  fetchDevelopmentSettings,
  clearPendingChanges
};

class DevelopmentSettingsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchDevelopmentSettings();
  }

  componentWillUnmount() {
    this.props.clearPendingChanges({ section: `settings.${SECTION}` });
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setDevelopmentSettingsValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveDevelopmentSettings();
  };

  //
  // Render

  render() {
    return (
      <DevelopmentSettings
        onInputChange={this.onInputChange}
        onSavePress={this.onSavePress}
        {...this.props}
      />
    );
  }
}

DevelopmentSettingsConnector.propTypes = {
  setDevelopmentSettingsValue: PropTypes.func.isRequired,
  saveDevelopmentSettings: PropTypes.func.isRequired,
  fetchDevelopmentSettings: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DevelopmentSettingsConnector);
