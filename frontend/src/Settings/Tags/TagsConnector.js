import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchApplications, fetchIndexerProxies, fetchNotifications } from 'Store/Actions/settingsActions';
import { fetchTagDetails } from 'Store/Actions/tagActions';
import Tags from './Tags';

function createMapStateToProps() {
  return createSelector(
    (state) => state.tags,
    (tags) => {
      const isFetching = tags.isFetching || tags.details.isFetching;
      const error = tags.error || tags.details.error;
      const isPopulated = tags.isPopulated && tags.details.isPopulated;

      return {
        ...tags,
        isFetching,
        error,
        isPopulated
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchTagDetails: fetchTagDetails,
  dispatchFetchNotifications: fetchNotifications,
  dispatchFetchIndexerProxies: fetchIndexerProxies,
  dispatchFetchApplications: fetchApplications
};

class MetadatasConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      dispatchFetchTagDetails,
      dispatchFetchNotifications,
      dispatchFetchIndexerProxies,
      dispatchFetchApplications
    } = this.props;

    dispatchFetchTagDetails();
    dispatchFetchNotifications();
    dispatchFetchIndexerProxies();
    dispatchFetchApplications();
  }

  //
  // Render

  render() {
    return (
      <Tags
        {...this.props}
      />
    );
  }
}

MetadatasConnector.propTypes = {
  dispatchFetchTagDetails: PropTypes.func.isRequired,
  dispatchFetchNotifications: PropTypes.func.isRequired,
  dispatchFetchIndexerProxies: PropTypes.func.isRequired,
  dispatchFetchApplications: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(MetadatasConnector);
