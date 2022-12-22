import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { grabRelease } from 'Store/Actions/releaseActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import SearchIndexOverviews from './SearchIndexOverviews';

function createMapStateToProps() {
  return createSelector(
    createUISettingsSelector(),
    createDimensionsSelector(),
    (uiSettings, dimensions) => {
      return {
        showRelativeDates: uiSettings.showRelativeDates,
        shortDateFormat: uiSettings.shortDateFormat,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat,
        isSmallScreen: dimensions.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGrabPress(payload) {
      dispatch(grabRelease(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(SearchIndexOverviews);
