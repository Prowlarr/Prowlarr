import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchIndexerSchema, selectIndexerSchema, setIndexerSchemaSort } from 'Store/Actions/indexerActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import AddIndexerModalContent from './AddIndexerModalContent';

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('indexers.schema'),
    (indexers) => {
      const {
        isFetching,
        isPopulated,
        error,
        items,
        sortDirection,
        sortKey
      } = indexers;

      return {
        isFetching,
        isPopulated,
        error,
        indexers: items,
        sortKey,
        sortDirection
      };
    }
  );
}

const mapDispatchToProps = {
  fetchIndexerSchema,
  selectIndexerSchema,
  setIndexerSchemaSort
};

class AddIndexerModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchIndexerSchema();
  }

  //
  // Listeners

  onIndexerSelect = ({ implementation, name }) => {
    this.props.selectIndexerSchema({ implementation, name });
    this.props.onSelectIndexer();
  };

  onSortPress = (sortKey, sortDirection) => {
    this.props.setIndexerSchemaSort({ sortKey, sortDirection });
  };

  //
  // Render

  render() {
    return (
      <AddIndexerModalContent
        {...this.props}
        onSortPress={this.onSortPress}
        onIndexerSelect={this.onIndexerSelect}
      />
    );
  }
}

AddIndexerModalContentConnector.propTypes = {
  fetchIndexerSchema: PropTypes.func.isRequired,
  selectIndexerSchema: PropTypes.func.isRequired,
  setIndexerSchemaSort: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSelectIndexer: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddIndexerModalContentConnector);
