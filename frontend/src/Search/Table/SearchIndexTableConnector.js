import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { grabRelease, saveRelease, setReleasesSort } from 'Store/Actions/releaseActions';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import SearchIndexTable from './SearchIndexTable';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    createUISettingsSelector(),
    (dimensions, uiSettings) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onSortPress(sortKey) {
      dispatch(setReleasesSort({ sortKey }));
    },
    onGrabPress(payload) {
      dispatch(grabRelease(payload));
    },
    onSavePress(payload) {
      dispatch(saveRelease(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(SearchIndexTable);
