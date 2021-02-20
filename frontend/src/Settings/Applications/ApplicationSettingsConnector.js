import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { testAllApplications } from 'Store/Actions/settingsActions';
import ApplicationSettings from './ApplicationSettings';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.applications.isTestingAll,
    (isTestingAll) => {
      return {
        isTestingAll
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchTestAllApplications: testAllApplications
};

export default connect(createMapStateToProps, mapDispatchToProps)(ApplicationSettings);
