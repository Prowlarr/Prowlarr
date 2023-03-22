import { find, isArray } from 'lodash-es';
import { createSelector } from 'reselect';
import selectSettings from 'Store/Selectors/selectSettings';

function createIndexerSchemaSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.indexers,
    (id, section) => {
      if (!id) {
        const item = isArray(section.schema.items) ? section.selectedSchema : section.schema.items;
        const settings = selectSettings(Object.assign({ name: '' }, item), section.pendingChanges, section.saveError);

        const {
          isSaving,
          saveError,
          isTesting,
          pendingChanges
        } = section;

        const {
          isFetching,
          isPopulated,
          error
        } = section.schema;

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

export default createIndexerSchemaSelector;
