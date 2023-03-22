import { find, reduce } from 'lodash-es';
import getSectionState from 'Utilities/State/getSectionState';

function getProviderState(payload, getState, section, keyValueOnly=true) {
  const {
    id,
    ...otherPayload
  } = payload;

  const state = getSectionState(getState(), section, true);
  const pendingChanges = Object.assign({}, state.pendingChanges, otherPayload);
  const pendingFields = state.pendingChanges.fields || {};
  delete pendingChanges.fields;

  const item = id ? find(state.items, { id }) : state.selectedSchema || state.schema || state.schema && state.schema.items || {};

  if (item.fields) {
    pendingChanges.fields = reduce(item.fields, (result, field) => {
      const name = field.name;

      const value = pendingFields.hasOwnProperty(name) ?
        pendingFields[name] :
        field.value;

      // Only send the name and value to the server
      if (keyValueOnly) {
        result.push({
          name,
          value
        });
      } else {
        result.push({
          ...field,
          value
        });
      }

      return result;
    }, []);
  }

  const result = Object.assign({}, item, pendingChanges);

  delete result.presets;

  return result;
}

export default getProviderState;
