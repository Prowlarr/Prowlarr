import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import { executeCommand } from 'Store/Actions/commandActions';
import { testAllApplications } from 'Store/Actions/settingsActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import ApplicationSettings from './ApplicationSettings';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.applications.isTestingAll,
    createCommandExecutingSelector(commandNames.APP_INDEXER_SYNC),
    (isTestingAll, isSyncingIndexers) => {
      return {
        isTestingAll,
        isSyncingIndexers
      };
    }
  );
}

function mapDispatchToProps(dispatch, props) {
  return {
    onTestAllPress() {
      dispatch(testAllApplications());
    },
    onAppIndexerSyncPress() {
      dispatch(executeCommand({
        name: commandNames.APP_INDEXER_SYNC
      }));
    }
  };
}

export default connect(createMapStateToProps, mapDispatchToProps)(ApplicationSettings);
