import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteApplication, fetchApplications } from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByName from 'Utilities/Array/sortByName';
import Applications from './Applications';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.applications', sortByName),
    (applications) => applications
  );
}

const mapDispatchToProps = {
  fetchApplications,
  deleteApplication
};

class ApplicationsConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchApplications();
  }

  //
  // Listeners

  onConfirmDeleteApplication = (id) => {
    this.props.deleteApplication({ id });
  };

  //
  // Render

  render() {
    return (
      <Applications
        {...this.props}
        onConfirmDeleteApplication={this.onConfirmDeleteApplication}
      />
    );
  }
}

ApplicationsConnector.propTypes = {
  fetchApplications: PropTypes.func.isRequired,
  deleteApplication: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ApplicationsConnector);
