import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AddIndexerModalContent from './AddIndexerModalContent';

class AddIndexerModalContentConnector extends Component {
  //
  // Render

  render() {
    return (
      <AddIndexerModalContent
        {...this.props}
      />
    );
  }
}

AddIndexerModalContentConnector.propTypes = {
  fetchIndexerSchema: PropTypes.func.isRequired,
  selectIndexerSchema: PropTypes.func.isRequired,
  setIndexerSchemaSort: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddIndexerModalContentConnector;
