import { cloneDeep, find, isFunction } from 'lodash-es';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';

function applySchemaDefaults(selectedSchema, schemaDefaults) {
  if (!schemaDefaults) {
    return selectedSchema;
  } else if (isFunction(schemaDefaults)) {
    return schemaDefaults(selectedSchema);
  }

  return Object.assign(selectedSchema, schemaDefaults);
}

function selectProviderSchema(state, section, payload, schemaDefaults) {
  const newState = getSectionState(state, section);

  const {
    implementation,
    presetName
  } = payload;

  const selectedImplementation = find(newState.schema, { implementation });

  const selectedSchema = presetName ?
    find(selectedImplementation.presets, { name: presetName }) :
    selectedImplementation;

  newState.selectedSchema = applySchemaDefaults(cloneDeep(selectedSchema), schemaDefaults);

  return updateSectionState(state, section, newState);
}

export default selectProviderSchema;
