import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withCurrentPage from 'Components/withCurrentPage';
import { executeCommand } from 'Store/Actions/commandActions';
import * as historyActions from 'Store/Actions/historyActions';
import { createCustomFiltersSelector } from 'Store/Selectors/createClientSideCollectionSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import { registerPagePopulator, unregisterPagePopulator } from 'Utilities/pagePopulator';
import History from './History';

function createMapStateToProps() {
  return createSelector(
    (state) => state.history,
    (state) => state.indexers,
    createCustomFiltersSelector('history'),
    createCommandExecutingSelector(commandNames.CLEAR_HISTORY),
    (history, indexers, customFilters, isHistoryClearing) => {
      return {
        isIndexersFetching: indexers.isFetching,
        isIndexersPopulated: indexers.isPopulated,
        indexersError: indexers.error,
        isHistoryClearing,
        customFilters,
        ...history
      };
    }
  );
}

const mapDispatchToProps = {
  executeCommand,
  ...historyActions
};

class HistoryConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      useCurrentPage,
      fetchHistory,
      gotoHistoryFirstPage
    } = this.props;

    registerPagePopulator(this.repopulate);

    if (useCurrentPage) {
      fetchHistory();
    } else {
      gotoHistoryFirstPage();
    }
  }

  componentDidUpdate(prevProps) {
    if (prevProps.isHistoryClearing && !this.props.isHistoryClearing) {
      this.props.gotoHistoryFirstPage();
    }
  }

  componentWillUnmount() {
    unregisterPagePopulator(this.repopulate);
    this.props.clearHistory();
  }

  //
  // Control

  repopulate = () => {
    this.props.fetchHistory();
  };

  //
  // Listeners

  onFirstPagePress = () => {
    this.props.gotoHistoryFirstPage();
  };

  onPreviousPagePress = () => {
    this.props.gotoHistoryPreviousPage();
  };

  onNextPagePress = () => {
    this.props.gotoHistoryNextPage();
  };

  onLastPagePress = () => {
    this.props.gotoHistoryLastPage();
  };

  onPageSelect = (page) => {
    this.props.gotoHistoryPage({ page });
  };

  onSortPress = (sortKey) => {
    this.props.setHistorySort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey) => {
    this.props.setHistoryFilter({ selectedFilterKey });
  };

  onClearHistoryPress = () => {
    this.props.executeCommand({ name: commandNames.CLEAR_HISTORY });
  };

  onTableOptionChange = (payload) => {
    this.props.setHistoryTableOption(payload);

    if (payload.pageSize) {
      this.props.gotoHistoryFirstPage();
    }
  };

  //
  // Render

  render() {
    return (
      <History
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onTableOptionChange={this.onTableOptionChange}
        onClearHistoryPress={this.onClearHistoryPress}
        {...this.props}
      />
    );
  }
}

HistoryConnector.propTypes = {
  useCurrentPage: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  fetchHistory: PropTypes.func.isRequired,
  clearHistory: PropTypes.func.isRequired,
  isHistoryClearing: PropTypes.bool.isRequired,
  gotoHistoryFirstPage: PropTypes.func.isRequired,
  gotoHistoryPreviousPage: PropTypes.func.isRequired,
  gotoHistoryNextPage: PropTypes.func.isRequired,
  gotoHistoryLastPage: PropTypes.func.isRequired,
  gotoHistoryPage: PropTypes.func.isRequired,
  setHistorySort: PropTypes.func.isRequired,
  setHistoryFilter: PropTypes.func.isRequired,
  setHistoryTableOption: PropTypes.func.isRequired,
  executeCommand: PropTypes.func.isRequired
};

export default withCurrentPage(
  connect(createMapStateToProps, mapDispatchToProps)(HistoryConnector)
);
