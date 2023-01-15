import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import AddCategoryModal from './AddCategoryModal';

function createMapDispatchToProps(dispatch, props) {
  const section = 'settings.downloadClientCategories';

  return {
    dispatchClearPendingChanges() {
      dispatch(clearPendingChanges({ section }));
    }
  };
}

class AddCategoryModalConnector extends Component {

  //
  // Listeners

  onModalClose = () => {
    this.props.dispatchClearPendingChanges();
    this.props.onModalClose();
  };

  //
  // Render

  render() {
    const {
      dispatchClearPendingChanges,
      ...otherProps
    } = this.props;

    return (
      <AddCategoryModal
        {...otherProps}
        onModalClose={this.onModalClose}
      />
    );
  }
}

AddCategoryModalConnector.propTypes = {
  onModalClose: PropTypes.func.isRequired,
  dispatchClearPendingChanges: PropTypes.func.isRequired
};

export default connect(null, createMapDispatchToProps)(AddCategoryModalConnector);
