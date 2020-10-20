import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchHistory, markAsFailed } from 'Store/Actions/historyActions';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import MovieHistoryRow from './MovieHistoryRow';

function createMapStateToProps() {
  return createSelector(
    createIndexerSelector(),
    createUISettingsSelector(),
    (movie, uiSettings) => {
      return {
        movie,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat
      };
    }
  );
}

const mapDispatchToProps = {
  fetchHistory,
  markAsFailed
};

export default connect(createMapStateToProps, mapDispatchToProps)(MovieHistoryRow);
