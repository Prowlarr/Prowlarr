import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { cancelSaveIndexerProxy, cancelTestIndexerProxy } from 'Store/Actions/settingsActions';
import EditIndexerProxyModal from './EditIndexerProxyModal';

function createMapDispatchToProps(dispatch, props) {
  const section = 'settings.indexerProxies';

  return {
    dispatchClearPendingChanges() {
      dispatch(clearPendingChanges({ section }));
    },

    dispatchCancelTestIndexerProxy() {
      dispatch(cancelTestIndexerProxy({ section }));
    },

    dispatchCancelSaveIndexerProxy() {
      dispatch(cancelSaveIndexerProxy({ section }));
    }
  };
}

class EditIndexerProxyModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.dispatchClearPendingChanges();
    this.props.dispatchCancelTestIndexerProxy();
    this.props.dispatchCancelSaveIndexerProxy();
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    const {
      dispatchClearPendingChanges,
      dispatchCancelTestIndexerProxy,
      dispatchCancelSaveIndexerProxy,
      ...otherProps
    } = this.props;

    return (
      <EditIndexerProxyModal
        {...otherProps}
        onModalClose={this.onModalClose}
      />
    );
  }
}

EditIndexerProxyModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  dispatchClearPendingChanges: PropTypes.func.isRequired,
  dispatchCancelTestIndexerProxy: PropTypes.func.isRequired,
  dispatchCancelSaveIndexerProxy: PropTypes.func.isRequired
};

export default connect(null, createMapDispatchToProps)(EditIndexerProxyModalConnector);
