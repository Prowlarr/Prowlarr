import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearDownloadClientCategoryPending, saveDownloadClientCategory, setDownloadClientCategoryFieldValue, setDownloadClientCategoryValue } from 'Store/Actions/settingsActions';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import AddCategoryModalContent from './AddCategoryModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createProviderSettingsSelector('downloadClientCategories'),
    (advancedSettings, specification) => {
      return {
        advancedSettings,
        ...specification
      };
    }
  );
}

const mapDispatchToProps = {
  setDownloadClientCategoryValue,
  setDownloadClientCategoryFieldValue,
  saveDownloadClientCategory,
  clearDownloadClientCategoryPending
};

class AddCategoryModalContentConnector extends Component {

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setDownloadClientCategoryValue({ name, value });
  };

  onFieldChange = ({ name, value }) => {
    this.props.setDownloadClientCategoryFieldValue({ name, value });
  };

  onCancelPress = () => {
    this.props.clearDownloadClientCategoryPending();
    this.props.onModalClose();
  };

  onSavePress = () => {
    this.props.saveDownloadClientCategory({ id: this.props.id });
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <AddCategoryModalContent
        {...this.props}
        onCancelPress={this.onCancelPress}
        onSavePress={this.onSavePress}
        onInputChange={this.onInputChange}
        onFieldChange={this.onFieldChange}
      />
    );
  }
}

AddCategoryModalContentConnector.propTypes = {
  id: PropTypes.number,
  item: PropTypes.object.isRequired,
  setDownloadClientCategoryValue: PropTypes.func.isRequired,
  setDownloadClientCategoryFieldValue: PropTypes.func.isRequired,
  clearDownloadClientCategoryPending: PropTypes.func.isRequired,
  saveDownloadClientCategory: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddCategoryModalContentConnector);
