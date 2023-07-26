import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler, { createCancelSaveProviderHandler } from 'Store/Actions/Creators/createSaveProviderHandler';
import createTestProviderHandler, { createCancelTestProviderHandler } from 'Store/Actions/Creators/createTestProviderHandler';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import selectProviderSchema from 'Utilities/State/selectProviderSchema';

//
// Variables

const section = 'settings.indexerProxies';

//
// Actions Types

export const FETCH_INDEXER_PROXYS = 'settings/indexerProxies/fetchIndexerProxies';
export const FETCH_INDEXER_PROXY_SCHEMA = 'settings/indexerProxies/fetchIndexerProxySchema';
export const SELECT_INDEXER_PROXY_SCHEMA = 'settings/indexerProxies/selectIndexerProxySchema';
export const SET_INDEXER_PROXY_VALUE = 'settings/indexerProxies/setIndexerProxyValue';
export const SET_INDEXER_PROXY_FIELD_VALUE = 'settings/indexerProxies/setIndexerProxyFieldValue';
export const SAVE_INDEXER_PROXY = 'settings/indexerProxies/saveIndexerProxy';
export const CANCEL_SAVE_INDEXER_PROXY = 'settings/indexerProxies/cancelSaveIndexerProxy';
export const DELETE_INDEXER_PROXY = 'settings/indexerProxies/deleteIndexerProxy';
export const TEST_INDEXER_PROXY = 'settings/indexerProxies/testIndexerProxy';
export const CANCEL_TEST_INDEXER_PROXY = 'settings/indexerProxies/cancelTestIndexerProxy';

//
// Action Creators

export const fetchIndexerProxies = createThunk(FETCH_INDEXER_PROXYS);
export const fetchIndexerProxySchema = createThunk(FETCH_INDEXER_PROXY_SCHEMA);
export const selectIndexerProxySchema = createAction(SELECT_INDEXER_PROXY_SCHEMA);

export const saveIndexerProxy = createThunk(SAVE_INDEXER_PROXY);
export const cancelSaveIndexerProxy = createThunk(CANCEL_SAVE_INDEXER_PROXY);
export const deleteIndexerProxy = createThunk(DELETE_INDEXER_PROXY);
export const testIndexerProxy = createThunk(TEST_INDEXER_PROXY);
export const cancelTestIndexerProxy = createThunk(CANCEL_TEST_INDEXER_PROXY);

export const setIndexerProxyValue = createAction(SET_INDEXER_PROXY_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setIndexerProxyFieldValue = createAction(SET_INDEXER_PROXY_FIELD_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Details

export default {

  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    isSchemaFetching: false,
    isSchemaPopulated: false,
    schemaError: null,
    schema: [],
    selectedSchema: {},
    isSaving: false,
    saveError: null,
    isDeleting: false,
    deleteError: null,
    isTesting: false,
    items: [],
    pendingChanges: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_INDEXER_PROXYS]: createFetchHandler(section, '/indexerProxy'),
    [FETCH_INDEXER_PROXY_SCHEMA]: createFetchSchemaHandler(section, '/indexerProxy/schema'),

    [SAVE_INDEXER_PROXY]: createSaveProviderHandler(section, '/indexerProxy'),
    [CANCEL_SAVE_INDEXER_PROXY]: createCancelSaveProviderHandler(section),
    [DELETE_INDEXER_PROXY]: createRemoveItemHandler(section, '/indexerProxy'),
    [TEST_INDEXER_PROXY]: createTestProviderHandler(section, '/indexerProxy'),
    [CANCEL_TEST_INDEXER_PROXY]: createCancelTestProviderHandler(section)
  },

  //
  // Reducers

  reducers: {
    [SET_INDEXER_PROXY_VALUE]: createSetSettingValueReducer(section),
    [SET_INDEXER_PROXY_FIELD_VALUE]: createSetProviderFieldValueReducer(section),

    [SELECT_INDEXER_PROXY_SCHEMA]: (state, { payload }) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        selectedSchema.name = selectedSchema.implementationName;

        return selectedSchema;
      });
    }
  }

};
