import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditAppProfileModal from './EditAppProfileModal';

function mapStateToProps() {
  return {};
}

const mapDispatchToProps = {
  clearPendingChanges
};

class EditAppProfileModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.clearPendingChanges({ section: 'settings.appProfiles' });
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    return (
      <EditAppProfileModal
        {...this.props}
        onModalClose={this.onModalClose}
      />
    );
  }
}

EditAppProfileModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(mapStateToProps, mapDispatchToProps)(EditAppProfileModalConnector);
