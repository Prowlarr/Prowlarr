import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  saveApplication,
  setApplicationFieldValue,
  setApplicationValue,
  testApplication,
  toggleAdvancedSettings
} from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import EditApplicationModalContent from './EditApplicationModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createProviderSettingsSelector('applications'),
    (advancedSettings, application) => {
      return {
        advancedSettings,
        ...application
      };
    }
  );
}

const mapDispatchToProps = {
  setApplicationValue,
  setApplicationFieldValue,
  saveApplication,
  testApplication,
  toggleAdvancedSettings
};

class EditApplicationModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setApplicationValue({ name, value });
  };

  onFieldChange = ({ name, value }) => {
    this.props.setApplicationFieldValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveApplication({ id: this.props.id });
  };

  onTestPress = () => {
    this.props.testApplication({ id: this.props.id });
  };

  onAdvancedSettingsPress = () => {
    this.props.toggleAdvancedSettings();
  };

  //
  // Render

  render() {
    return (
      <EditApplicationModalContent
        {...this.props}
        onSavePress={this.onSavePress}
        onTestPress={this.onTestPress}
        onInputChange={this.onInputChange}
        onFieldChange={this.onFieldChange}
        onAdvancedSettingsPress={this.onAdvancedSettingsPress}
      />
    );
  }
}

EditApplicationModalContentConnector.propTypes = {
  id: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  setApplicationValue: PropTypes.func,
  setApplicationFieldValue: PropTypes.func,
  saveApplication: PropTypes.func,
  testApplication: PropTypes.func,
  onModalClose: PropTypes.func.isRequired,
  toggleAdvancedSettings: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditApplicationModalContentConnector);
