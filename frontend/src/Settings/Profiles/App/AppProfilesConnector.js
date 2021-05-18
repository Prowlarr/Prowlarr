import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { cloneAppProfile, deleteAppProfile, fetchAppProfiles } from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByName from 'Utilities/Array/sortByName';
import AppProfiles from './AppProfiles';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.appProfiles', sortByName),
    (appProfiles) => appProfiles
  );
}

const mapDispatchToProps = {
  dispatchFetchAppProfiles: fetchAppProfiles,
  dispatchDeleteAppProfile: deleteAppProfile,
  dispatchCloneAppProfile: cloneAppProfile
};

class AppProfilesConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchAppProfiles();
  }

  //
  // Listeners

  onConfirmDeleteAppProfile = (id) => {
    this.props.dispatchDeleteAppProfile({ id });
  }

  onCloneAppProfilePress = (id) => {
    this.props.dispatchCloneAppProfile({ id });
  }

  //
  // Render

  render() {
    return (
      <AppProfiles
        onConfirmDeleteAppProfile={this.onConfirmDeleteAppProfile}
        onCloneAppProfilePress={this.onCloneAppProfilePress}
        {...this.props}
      />
    );
  }
}

AppProfilesConnector.propTypes = {
  dispatchFetchAppProfiles: PropTypes.func.isRequired,
  dispatchDeleteAppProfile: PropTypes.func.isRequired,
  dispatchCloneAppProfile: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AppProfilesConnector);
