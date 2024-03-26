import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  deleteDownloadClientCategory,
  fetchDownloadClientCategories,
  saveDownloadClient,
  setDownloadClientFieldValue,
  setDownloadClientValue,
  testDownloadClient,
  toggleAdvancedSettings
} from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import EditDownloadClientModalContent from './EditDownloadClientModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createProviderSettingsSelector('downloadClients'),
    (state) => state.settings.downloadClientCategories,
    (advancedSettings, downloadClient, categories) => {
      return {
        advancedSettings,
        ...downloadClient,
        categories: categories.items
      };
    }
  );
}

const mapDispatchToProps = {
  setDownloadClientValue,
  setDownloadClientFieldValue,
  saveDownloadClient,
  testDownloadClient,
  fetchDownloadClientCategories,
  deleteDownloadClientCategory,
  toggleAdvancedSettings
};

class EditDownloadClientModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      id,
      tagsFromId
    } = this.props;
    this.props.fetchDownloadClientCategories({ id: tagsFromId || id });
  }

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setDownloadClientValue({ name, value });
  };

  onFieldChange = ({ name, value }) => {
    this.props.setDownloadClientFieldValue({ name, value });
  };

  onSavePress = () => {
    this.props.saveDownloadClient({ id: this.props.id });
  };

  onTestPress = () => {
    this.props.testDownloadClient({ id: this.props.id });
  };

  onAdvancedSettingsPress = () => {
    this.props.toggleAdvancedSettings();
  };

  onConfirmDeleteCategory = (id) => {
    this.props.deleteDownloadClientCategory({ id });
  };

  //
  // Render

  render() {
    return (
      <EditDownloadClientModalContent
        {...this.props}
        onSavePress={this.onSavePress}
        onTestPress={this.onTestPress}
        onAdvancedSettingsPress={this.onAdvancedSettingsPress}
        onInputChange={this.onInputChange}
        onFieldChange={this.onFieldChange}
        onConfirmDeleteCategory={this.onConfirmDeleteCategory}
      />
    );
  }
}

EditDownloadClientModalContentConnector.propTypes = {
  id: PropTypes.number,
  tagsFromId: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  fetchDownloadClientCategories: PropTypes.func.isRequired,
  deleteDownloadClientCategory: PropTypes.func.isRequired,
  setDownloadClientValue: PropTypes.func.isRequired,
  setDownloadClientFieldValue: PropTypes.func.isRequired,
  saveDownloadClient: PropTypes.func.isRequired,
  testDownloadClient: PropTypes.func.isRequired,
  toggleAdvancedSettings: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditDownloadClientModalContentConnector);
