import { push } from 'connected-react-router';
import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchHistory, markAsFailed } from 'Store/Actions/historyActions';
import { setSearchDefault } from 'Store/Actions/releaseActions';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import HistoryRow from './HistoryRow';

function createMapStateToProps() {
  return createSelector(
    createIndexerSelector(),
    createUISettingsSelector(),
    (indexer, uiSettings) => {
      return {
        indexer,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat
      };
    }
  );
}

const mapDispatchToProps = {
  fetchHistory,
  markAsFailed,
  setSearchDefault,
  push
};

class HistoryRowConnector extends Component {

  //
  // Lifecycle

  componentDidUpdate(prevProps) {
    if (
      prevProps.isMarkingAsFailed &&
      !this.props.isMarkingAsFailed &&
      !this.props.markAsFailedError
    ) {
      this.props.fetchHistory();
    }
  }

  //
  // Listeners

  onSearchPress = (query, indexerId, categories, type, limit, offset) => {
    this.props.setSearchDefault(_.pickBy({
      searchQuery: query,
      searchIndexerIds: [indexerId],
      searchCategories: categories,
      searchType: type,
      searchLimit: limit,
      searchOffset: offset
    }));
    this.props.push(`${window.Prowlarr.urlBase}/search`);
  };

  onMarkAsFailedPress = () => {
    this.props.markAsFailed({ id: this.props.id });
  };

  //
  // Render

  render() {
    return (
      <HistoryRow
        {...this.props}
        onMarkAsFailedPress={this.onMarkAsFailedPress}
        onSearchPress={this.onSearchPress}
      />
    );
  }

}

HistoryRowConnector.propTypes = {
  id: PropTypes.number.isRequired,
  isMarkingAsFailed: PropTypes.bool,
  markAsFailedError: PropTypes.object,
  fetchHistory: PropTypes.func.isRequired,
  markAsFailed: PropTypes.func.isRequired,
  setSearchDefault: PropTypes.func.isRequired,
  push: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(HistoryRowConnector);
