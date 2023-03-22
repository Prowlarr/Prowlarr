import { find, isArray } from 'lodash-es';
import { createSelector } from 'reselect';
import selectSettings from 'Store/Selectors/selectSettings';

function createProviderSelector(sectionName) {
  return createSelector(
    (state, { id }) => id,
    (state) => state[sectionName],
    (id, section) => {
      if (!id) {
        const item = isArray(section.schema) ? section.selectedSchema : section.schema;
        const settings = selectSettings(Object.assign({ name: '' }, item), section.pendingChanges, section.saveError);

        const {
          isSchemaFetching: isFetching,
          isSchemaPopulated: isPopulated,
          schemaError: error,
          isSaving,
          saveError,
          isTesting,
          pendingChanges
        } = section;

        return {
          isFetching,
          isPopulated,
          error,
          isSaving,
          saveError,
          isTesting,
          pendingChanges,
          ...settings,
          item: settings.settings
        };
      }

      const {
        isFetching,
        isPopulated,
        error,
        isSaving,
        saveError,
        isTesting,
        pendingChanges
      } = section;

      const settings = selectSettings(find(section.items, { id }), pendingChanges, saveError);

      return {
        isFetching,
        isPopulated,
        error,
        isSaving,
        saveError,
        isTesting,
        ...settings,
        item: settings.settings
      };
    }
  );
}

export default createProviderSelector;
