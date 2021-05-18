import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchAppProfileSchema, saveAppProfile, setAppProfileValue } from 'Store/Actions/settingsActions';
import createProfileInUseSelector from 'Store/Selectors/createProfileInUseSelector';
import createProviderSettingsSelector from 'Store/Selectors/createProviderSettingsSelector';
import EditAppProfileModalContent from './EditAppProfileModalContent';

function createMapStateToProps() {
  return createSelector(
    createProviderSettingsSelector('appProfiles'),
    createProfileInUseSelector('appProfileId'),
    (appProfile, isInUse) => {
      return {
        ...appProfile,
        isInUse
      };
    }
  );
}

const mapDispatchToProps = {
  fetchAppProfileSchema,
  setAppProfileValue,
  saveAppProfile
};

class EditAppProfileModalContentConnector extends Component {

  componentDidMount() {
    if (!this.props.id && !this.props.isPopulated) {
      this.props.fetchAppProfileSchema();
    }
  }

  componentDidUpdate(prevProps, prevState) {
    if (prevProps.isSaving && !this.props.isSaving && !this.props.saveError) {
      this.props.onModalClose();
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setAppProfileValue({ name, value });
  }

  onSavePress = () => {
    this.props.saveAppProfile({ id: this.props.id });
  }

  //
  // Render

  render() {
    return (
      <EditAppProfileModalContent
        {...this.state}
        {...this.props}
        onSavePress={this.onSavePress}
        onInputChange={this.onInputChange}
      />
    );
  }
}

EditAppProfileModalContentConnector.propTypes = {
  id: PropTypes.number,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  item: PropTypes.object.isRequired,
  setAppProfileValue: PropTypes.func.isRequired,
  fetchAppProfileSchema: PropTypes.func.isRequired,
  saveAppProfile: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(EditAppProfileModalContentConnector);
