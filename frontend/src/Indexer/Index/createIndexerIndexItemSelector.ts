import { createSelector } from 'reselect';
import Indexer from 'Indexer/Indexer';
import createIndexerAppProfileSelector from 'Store/Selectors/createIndexerAppProfileSelector';
import { createIndexerSelectorForHook } from 'Store/Selectors/createIndexerSelector';
import createIndexerStatusSelector from 'Store/Selectors/createIndexerStatusSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';

function createIndexerIndexItemSelector(indexerId: number) {
  return createSelector(
    createIndexerSelectorForHook(indexerId),
    createIndexerAppProfileSelector(indexerId),
    createIndexerStatusSelector(indexerId),
    createUISettingsSelector(),
    (indexer: Indexer, appProfile, status, uiSettings) => {
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
