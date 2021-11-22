import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { cancelSaveApplication, cancelTestApplication } from 'Store/Actions/settingsActions';
import EditApplicationModal from './EditApplicationModal';

function createMapDispatchToProps(dispatch, props) {
  const section = 'settings.applications';

  return {
    dispatchClearPendingChanges() {
      dispatch(clearPendingChanges({ section }));
    },

    dispatchCancelTestApplication() {
      dispatch(cancelTestApplication({ section }));
    },

    dispatchCancelSaveApplication() {
      dispatch(cancelSaveApplication({ section }));
    }
  };
}

class EditApplicationModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.dispatchClearPendingChanges();
    this.props.dispatchCancelTestApplication();
    this.props.dispatchCancelSaveApplication();
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    const {
      dispatchClearPendingChanges,
      dispatchCancelTestApplication,
      dispatchCancelSaveApplication,
      ...otherProps
    } = this.props;

    return (
      <EditApplicationModal
        {...otherProps}
        onModalClose={this.onModalClose}
      />
    );
  }
}

EditApplicationModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  dispatchClearPendingChanges: PropTypes.func.isRequired,
  dispatchCancelTestApplication: PropTypes.func.isRequired,
  dispatchCancelSaveApplication: PropTypes.func.isRequired
};

export default connect(null, createMapDispatchToProps)(EditApplicationModalConnector);
