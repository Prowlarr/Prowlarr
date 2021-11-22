import { push } from 'connected-react-router';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteIndexer } from 'Store/Actions/indexerActions';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import DeleteIndexerModalContent from './DeleteIndexerModalContent';

function createMapStateToProps() {
  return createSelector(
    createIndexerSelector(),
    (indexer) => {
      return indexer;
    }
  );
}

const mapDispatchToProps = {
  deleteIndexer,
  push
};

class DeleteIndexerModalContentConnector extends Component {

  //
  // Listeners

  onDeletePress = () => {
    this.props.deleteIndexer({
      id: this.props.indexerId
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <DeleteIndexerModalContent
        {...this.props}
        onDeletePress={this.onDeletePress}
      />
    );
  }
}

DeleteIndexerModalContentConnector.propTypes = {
  indexerId: PropTypes.number.isRequired,
  onModalClose: PropTypes.func.isRequired,
  deleteIndexer: PropTypes.func.isRequired,
  push: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(DeleteIndexerModalContentConnector);
