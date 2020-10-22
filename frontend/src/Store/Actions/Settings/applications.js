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

const section = 'settings.applications';

//
// Actions Types

export const FETCH_APPLICATIONS = 'settings/applications/fetchApplications';
export const FETCH_APPLICATION_SCHEMA = 'settings/applications/fetchApplicationSchema';
export const SELECT_APPLICATION_SCHEMA = 'settings/applications/selectApplicationSchema';
export const SET_APPLICATION_VALUE = 'settings/applications/setApplicationValue';
export const SET_APPLICATION_FIELD_VALUE = 'settings/applications/setApplicationFieldValue';
export const SAVE_APPLICATION = 'settings/applications/saveApplication';
export const CANCEL_SAVE_APPLICATION = 'settings/applications/cancelSaveApplication';
export const DELETE_APPLICATION = 'settings/applications/deleteApplication';
export const TEST_APPLICATION = 'settings/applications/testApplication';
export const CANCEL_TEST_APPLICATION = 'settings/applications/cancelTestApplication';

//
// Action Creators

export const fetchApplications = createThunk(FETCH_APPLICATIONS);
export const fetchApplicationSchema = createThunk(FETCH_APPLICATION_SCHEMA);
export const selectApplicationSchema = createAction(SELECT_APPLICATION_SCHEMA);

export const saveApplication = createThunk(SAVE_APPLICATION);
export const cancelSaveApplication = createThunk(CANCEL_SAVE_APPLICATION);
export const deleteApplication = createThunk(DELETE_APPLICATION);
export const testApplication = createThunk(TEST_APPLICATION);
export const cancelTestApplication = createThunk(CANCEL_TEST_APPLICATION);

export const setApplicationValue = createAction(SET_APPLICATION_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setApplicationFieldValue = createAction(SET_APPLICATION_FIELD_VALUE, (payload) => {
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
    isTesting: false,
    items: [],
    pendingChanges: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_APPLICATIONS]: createFetchHandler(section, '/applications'),
    [FETCH_APPLICATION_SCHEMA]: createFetchSchemaHandler(section, '/applications/schema'),

    [SAVE_APPLICATION]: createSaveProviderHandler(section, '/applications'),
    [CANCEL_SAVE_APPLICATION]: createCancelSaveProviderHandler(section),
    [DELETE_APPLICATION]: createRemoveItemHandler(section, '/applications'),
    [TEST_APPLICATION]: createTestProviderHandler(section, '/applications'),
    [CANCEL_TEST_APPLICATION]: createCancelTestProviderHandler(section)
  },

  //
  // Reducers

  reducers: {
    [SET_APPLICATION_VALUE]: createSetSettingValueReducer(section),
    [SET_APPLICATION_FIELD_VALUE]: createSetProviderFieldValueReducer(section),

    [SELECT_APPLICATION_SCHEMA]: (state, { payload }) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        selectedSchema.onGrab = selectedSchema.supportsOnGrab;
        selectedSchema.onDownload = selectedSchema.supportsOnDownload;
        selectedSchema.onUpgrade = selectedSchema.supportsOnUpgrade;
        selectedSchema.onRename = selectedSchema.supportsOnRename;

        return selectedSchema;
      });
    }
  }

};
