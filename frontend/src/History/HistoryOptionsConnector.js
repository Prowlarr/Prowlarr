import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { saveGeneralSettings } from 'Store/Actions/settingsActions';
import HistoryOptions from './HistoryOptions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.general.item,
    (generalSettings) => {
      return {
        ...generalSettings
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSaveGeneralSettings: saveGeneralSettings
};

export default connect(createMapStateToProps, mapDispatchToProps)(HistoryOptions);
