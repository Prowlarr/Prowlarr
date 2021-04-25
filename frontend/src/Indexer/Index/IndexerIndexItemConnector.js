import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { executeCommand } from 'Store/Actions/commandActions';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import createIndexerStatusSelector from 'Store/Selectors/createIndexerStatusSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';

function selectShowSearchAction() {
  return createSelector(
    (state) => state.indexerIndex,
    (indexerIndex) => {
      return indexerIndex.tableOptions.showSearchAction;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createIndexerSelector(),
    createIndexerStatusSelector(),
    selectShowSearchAction(),
    createUISettingsSelector(),
    (
      movie,
      status,
      showSearchAction,
      uiSettings
    ) => {

      // If a movie is deleted this selector may fire before the parent
      // selecors, which will result in an undefined movie, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a movie that has no information available.

      if (!movie) {
        return {};
      }

      return {
        ...movie,
        status,
        showSearchAction,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchExecuteCommand: executeCommand
};

class IndexerIndexItemConnector extends Component {

  //
  // Render

  render() {
    const {
      id,
      component: ItemComponent,
      ...otherProps
    } = this.props;

    if (!id) {
      return null;
    }

    return (
      <ItemComponent
        {...otherProps}
        id={id}
      />
    );
  }
}

IndexerIndexItemConnector.propTypes = {
  id: PropTypes.number,
  component: PropTypes.elementType.isRequired,
  dispatchExecuteCommand: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(IndexerIndexItemConnector);
