import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import IndexerInfoModalContent from './IndexerInfoModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.advancedSettings,
    createIndexerSelector(),
    (advancedSettings, indexer) => {
      console.log(indexer);
      return {
        advancedSettings,
        ...indexer
      };
    }
  );
}

class IndexerInfoModalContentConnector extends Component {

  //
  // Render

  render() {
    return (
      <IndexerInfoModalContent
        {...this.props}
      />
    );
  }
}

IndexerInfoModalContentConnector.propTypes = {
  indexerId: PropTypes.number,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps)(IndexerInfoModalContentConnector);
