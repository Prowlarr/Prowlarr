import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import createBulkEditItemHandler from 'Store/Actions/Creators/createBulkEditItemHandler';
import createBulkRemoveItemHandler from 'Store/Actions/Creators/createBulkRemoveItemHandler';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler, { createCancelSaveProviderHandler } from 'Store/Actions/Creators/createSaveProviderHandler';
import createTestAllProvidersHandler from 'Store/Actions/Creators/createTestAllProvidersHandler';
import createTestProviderHandler, { createCancelTestProviderHandler } from 'Store/Actions/Creators/createTestProviderHandler';
import createSetClientSideCollectionSortReducer from 'Store/Actions/Creators/Reducers/createSetClientSideCollectionSortReducer';
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
export const TEST_ALL_APPLICATIONS = 'settings/applications/testAllApplications';
export const BULK_EDIT_APPLICATIONS = 'settings/applications/bulkEditApplications';
export const BULK_DELETE_APPLICATIONS = 'settings/applications/bulkDeleteApplications';
export const SET_MANAGE_APPLICATIONS_SORT = 'settings/applications/setManageApplicationsSort';

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
export const testAllApplications = createThunk(TEST_ALL_APPLICATIONS);
export const bulkEditApplications = createThunk(BULK_EDIT_APPLICATIONS);
export const bulkDeleteApplications = createThunk(BULK_DELETE_APPLICATIONS);
export const setManageApplicationsSort = createAction(SET_MANAGE_APPLICATIONS_SORT);

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
    isDeleting: false,
    deleteError: null,
    isTesting: false,
    isTestingAll: false,
    items: [],
    pendingChanges: {},
    sortKey: 'name',
    sortDirection: sortDirections.ASCENDING,
    sortPredicates: {
      name: function(item) {
        return item.name.toLowerCase();
      }
    }
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
    [CANCEL_TEST_APPLICATION]: createCancelTestProviderHandler(section),
    [TEST_ALL_APPLICATIONS]: createTestAllProvidersHandler(section, '/applications'),
    [BULK_EDIT_APPLICATIONS]: createBulkEditItemHandler(section, '/applications/bulk'),
    [BULK_DELETE_APPLICATIONS]: createBulkRemoveItemHandler(section, '/applications/bulk')
  },

  //
  // Reducers

  reducers: {
    [SET_APPLICATION_VALUE]: createSetSettingValueReducer(section),
    [SET_APPLICATION_FIELD_VALUE]: createSetProviderFieldValueReducer(section),

    [SELECT_APPLICATION_SCHEMA]: (state, { payload }) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        selectedSchema.name = selectedSchema.implementationName;

        return selectedSchema;
      });
    },

    [SET_MANAGE_APPLICATIONS_SORT]: createSetClientSideCollectionSortReducer(section)

  }

};
