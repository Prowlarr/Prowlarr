import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  saveIndexerProxy,
  setIndexerProxyFieldValue,
  setIndexerProxyValue,
  testIndexerProxy,
  toggleAdvancedSettings
} from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import EditIndexerProxyModalContent from './EditIndexerProxyModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createProviderSettingsSelector('indexerProxies'),
    (advancedSettings, indexerProxy) => {
      return {
        advancedSettings,
        ...indexerProxy
      };
    }
  );
}

const mapDispatchToProps = {
  setIndexerProxyValue,
  setIndexerProxyFieldValue,
  saveIndexerProxy,
  testIndexerProxy,
  toggleAdvancedSettings
};

class EditIndexerProxyModalContentConnector extends Component {

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
    this.props.setIndexerProxyValue({ name, value });
  };

  onFieldChange = ({ name, value }) => {
    this.props.setIndexerProxyFieldValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveIndexerProxy({ id: this.props.id });
  };

  onTestPress = () => {
    this.props.testIndexerProxy({ id: this.props.id });
  };

  onAdvancedSettingsPress = () => {
    this.props.toggleAdvancedSettings();
  };

  //
  // Render

  render() {
    return (
      <EditIndexerProxyModalContent
        {...this.props}
        onSavePress={this.onSavePress}
        onTestPress={this.onTestPress}
        onAdvancedSettingsPress={this.onAdvancedSettingsPress}
        onInputChange={this.onInputChange}
        onFieldChange={this.onFieldChange}
      />
    );
  }
}

EditIndexerProxyModalContentConnector.propTypes = {
  id: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  setIndexerProxyValue: PropTypes.func.isRequired,
  setIndexerProxyFieldValue: PropTypes.func.isRequired,
  saveIndexerProxy: PropTypes.func.isRequired,
  testIndexerProxy: PropTypes.func.isRequired,
  toggleAdvancedSettings: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditIndexerProxyModalContentConnector);
