import _ from 'lodash';
import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler, { createCancelSaveProviderHandler } from 'Store/Actions/Creators/createSaveProviderHandler';
import createTestAllProvidersHandler from 'Store/Actions/Creators/createTestAllProvidersHandler';
import createTestProviderHandler, { createCancelTestProviderHandler } from 'Store/Actions/Creators/createTestProviderHandler';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk, handleThunks } from 'Store/thunks';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';

//
// Variables

export const section = 'indexers';
const schemaSection = `${section}.schema`;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  selectedSchema: {},
  isSaving: false,
  saveError: null,
  isTesting: false,
  isTestingAll: false,
  items: [],
  pendingChanges: {},

  schema: {
    isFetching: false,
    isPopulated: false,
    error: null,
    sortKey: 'name',
    sortDirection: sortDirections.ASCENDING,
    items: [],
    filteredIndexers: []
  }
};

export const filters = [
  {
    key: 'all',
    label: translate('All'),
    filters: []
  }
];

export const filterPredicates = {
  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  }
};

export const sortPredicates = {};

//
// Actions Types

export const FETCH_INDEXERS = 'indexers/fetchIndexers';
export const FETCH_INDEXER_SCHEMA = 'indexers/fetchIndexerSchema';
export const SELECT_INDEXER_SCHEMA = 'indexers/selectIndexerSchema';
export const SET_INDEXER_SCHEMA_SORT = 'indexers/setIndexerSchemaSort';
export const CLONE_INDEXER = 'indexers/cloneIndexer';
export const SET_INDEXER_VALUE = 'indexers/setIndexerValue';
export const SET_INDEXER_FIELD_VALUE = 'indexers/setIndexerFieldValue';
export const SAVE_INDEXER = 'indexers/saveIndexer';
export const CANCEL_SAVE_INDEXER = 'indexers/cancelSaveIndexer';
export const DELETE_INDEXER = 'indexers/deleteIndexer';
export const TEST_INDEXER = 'indexers/testIndexer';
export const CANCEL_TEST_INDEXER = 'indexers/cancelTestIndexer';
export const TEST_ALL_INDEXERS = 'indexers/testAllIndexers';
export const SET_FILTERED_INDEXERS = 'indexers/filtered';

//
// Action Creators

export const fetchIndexers = createThunk(FETCH_INDEXERS);
export const fetchIndexerSchema = createThunk(FETCH_INDEXER_SCHEMA);
export const selectIndexerSchema = createAction(SELECT_INDEXER_SCHEMA);
export const setIndexerSchemaSort = createAction(SET_INDEXER_SCHEMA_SORT);
export const cloneIndexer = createAction(CLONE_INDEXER);

export const saveIndexer = createThunk(SAVE_INDEXER);
export const cancelSaveIndexer = createThunk(CANCEL_SAVE_INDEXER);
export const deleteIndexer = createThunk(DELETE_INDEXER);
export const testIndexer = createThunk(TEST_INDEXER);
export const cancelTestIndexer = createThunk(CANCEL_TEST_INDEXER);
export const testAllIndexers = createThunk(TEST_ALL_INDEXERS);

export const setIndexerValue = createAction(SET_INDEXER_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setIndexerFieldValue = createAction(SET_INDEXER_FIELD_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Action Handlers

function applySchemaDefaults(selectedSchema, schemaDefaults) {
  if (!schemaDefaults) {
    return selectedSchema;
  } else if (_.isFunction(schemaDefaults)) {
    return schemaDefaults(selectedSchema);
  }

  return Object.assign(selectedSchema, schemaDefaults);
}

function selectSchema(state, payload, schemaDefaults) {
  const newState = getSectionState(state, section);

  const {
    implementation,
    name
  } = payload;

  const selectedImplementation = _.find(newState.schema.items, { implementation, name });

  newState.selectedSchema = applySchemaDefaults(_.cloneDeep(selectedImplementation), schemaDefaults);

  return updateSectionState(state, section, newState);
}

export const actionHandlers = handleThunks({
  [FETCH_INDEXERS]: createFetchHandler(section, '/indexer'),
  [FETCH_INDEXER_SCHEMA]: createFetchHandler(schemaSection, '/indexer/schema'),

  [SAVE_INDEXER]: createSaveProviderHandler(section, '/indexer'),
  [CANCEL_SAVE_INDEXER]: createCancelSaveProviderHandler(section),
  [DELETE_INDEXER]: createRemoveItemHandler(section, '/indexer'),
  [TEST_INDEXER]: createTestProviderHandler(section, '/indexer'),
  [CANCEL_TEST_INDEXER]: createCancelTestProviderHandler(section),
  [TEST_ALL_INDEXERS]: createTestAllProvidersHandler(section, '/indexer')
});

//
// Reducers

export const reducers = createHandleActions({
  [SET_INDEXER_VALUE]: createSetSettingValueReducer(section),
  [SET_INDEXER_FIELD_VALUE]: createSetProviderFieldValueReducer(section),
  [SET_INDEXER_SCHEMA_SORT]: createSetClientSideCollectionSortReducer(schemaSection),

  [SELECT_INDEXER_SCHEMA]: (state, { payload }) => {
    return selectSchema(state, payload, (selectedSchema) => {
      selectedSchema.enable = selectedSchema.supportsRss;

      return selectedSchema;
    });
  },

  [CLONE_INDEXER]: function(state, { payload }) {
    const id = payload.id;
    const newState = getSectionState(state, section);
    const item = newState.items.find((i) => i.id === id);

    // Use selectedSchema so `createProviderSettingsSelector` works properly
    const selectedSchema = { ...item };
    delete selectedSchema.id;
    delete selectedSchema.name;

    selectedSchema.fields = selectedSchema.fields.map((field) => {
      return { ...field };
    });

    newState.selectedSchema = selectedSchema;

    // Set the name in pendingChanges
    newState.pendingChanges = {
      name: `${item.name} - Copy`
    };

    return updateSectionState(state, section, newState);
  },

  [SET_FILTERED_INDEXERS]: function(state, { payload }) {
    const newState = { ...state, schema: { ...state.schema, filteredIndexers: payload } };

    return updateSectionState(state, section, newState);
  }
}, defaultState, section);
