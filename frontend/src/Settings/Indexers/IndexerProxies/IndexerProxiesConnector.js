import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteIndexerProxy, fetchIndexerProxies } from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import sortByName from 'Utilities/Array/sortByName';
import IndexerProxies from './IndexerProxies';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.indexerProxies', sortByName),
    createSortedSectionSelector('indexers', sortByName),
    createTagsSelector(),
    (indexerProxies, indexers, tagList) => {
      return {
        ...indexerProxies,
        indexerList: indexers.items,
        tagList
      };
    }
  );
}

const mapDispatchToProps = {
  fetchIndexerProxies,
  deleteIndexerProxy
};

class IndexerProxiesConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchIndexerProxies();
  }

  //
  // Listeners

  onConfirmDeleteIndexerProxy = (id) => {
    this.props.deleteIndexerProxy({ id });
  };

  //
  // Render

  render() {
    return (
      <IndexerProxies
        {...this.props}
        onConfirmDeleteIndexerProxy={this.onConfirmDeleteIndexerProxy}
      />
    );
  }
}

IndexerProxiesConnector.propTypes = {
  fetchIndexerProxies: PropTypes.func.isRequired,
  deleteIndexerProxy: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IndexerProxiesConnector);
