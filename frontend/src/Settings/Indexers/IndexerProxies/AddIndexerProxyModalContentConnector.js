import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchIndexerProxySchema, selectIndexerProxySchema } from 'Store/Actions/settingsActions';
import AddIndexerProxyModalContent from './AddIndexerProxyModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.indexerProxies,
    (indexerProxies) => {
      const {
        isSchemaFetching,
        isSchemaPopulated,
        schemaError,
        schema
      } = indexerProxies;

      return {
        isSchemaFetching,
        isSchemaPopulated,
        schemaError,
        schema
      };
    }
  );
}

const mapDispatchToProps = {
  fetchIndexerProxySchema,
  selectIndexerProxySchema
};

class AddIndexerProxyModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchIndexerProxySchema();
  }

  //
  // Listeners

  onIndexerProxySelect = ({ implementation, name }) => {
    this.props.selectIndexerProxySchema({ implementation, presetName: name });
    this.props.onModalClose({ indexerProxySelected: true });
  };

  //
  // Render

  render() {
    return (
      <AddIndexerProxyModalContent
        {...this.props}
        onIndexerProxySelect={this.onIndexerProxySelect}
      />
    );
  }
}

AddIndexerProxyModalContentConnector.propTypes = {
  fetchIndexerProxySchema: PropTypes.func.isRequired,
  selectIndexerProxySchema: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(AddIndexerProxyModalContentConnector);
