import { createSelector } from 'reselect';
import Indexer from 'Indexer/Indexer';
import createIndexerAppProfileSelector from 'Store/Selectors/createIndexerAppProfileSelector';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import createIndexerStatusSelector from 'Store/Selectors/createIndexerStatusSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';

function createIndexerIndexItemSelector(indexerId: number) {
  return createSelector(
    createIndexerSelector(indexerId),
    createIndexerAppProfileSelector(indexerId),
    createIndexerStatusSelector(indexerId),
    createUISettingsSelector(),
    (indexer: Indexer, appProfile, status, uiSettings) => {
      // If a series is deleted this selector may fire before the parent
      // selectors, which will result in an undefined series, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a series that has no information available.

      if (!indexer) {
        return {};
      }

      return {
        indexer,
        appProfile,
        status,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat,
      };
    }
  );
}

export default createIndexerIndexItemSelector;
