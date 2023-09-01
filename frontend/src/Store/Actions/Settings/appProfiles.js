import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createFetchSchemaHandler from 'Store/Actions/Creators/createFetchSchemaHandler';
import createRemoveItemHandler from 'Store/Actions/Creators/createRemoveItemHandler';
import createSaveProviderHandler from 'Store/Actions/Creators/createSaveProviderHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';

//
// Variables

const section = 'settings.appProfiles';

//
// Actions Types

export const FETCH_APP_PROFILES = 'settings/appProfiles/fetchAppProfiles';
export const FETCH_APP_PROFILE_SCHEMA = 'settings/appProfiles/fetchAppProfileSchema';
export const SAVE_APP_PROFILE = 'settings/appProfiles/saveAppProfile';
export const DELETE_APP_PROFILE = 'settings/appProfiles/deleteAppProfile';
export const SET_APP_PROFILE_VALUE = 'settings/appProfiles/setAppProfileValue';
export const CLONE_APP_PROFILE = 'settings/appProfiles/cloneAppProfile';

//
// Action Creators

export const fetchAppProfiles = createThunk(FETCH_APP_PROFILES);
export const fetchAppProfileSchema = createThunk(FETCH_APP_PROFILE_SCHEMA);
export const saveAppProfile = createThunk(SAVE_APP_PROFILE);
export const deleteAppProfile = createThunk(DELETE_APP_PROFILE);

export const setAppProfileValue = createAction(SET_APP_PROFILE_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const cloneAppProfile = createAction(CLONE_APP_PROFILE);

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
    schema: {},
    isSaving: false,
    saveError: null,
    isDeleting: false,
    deleteError: null,
    items: [],
    pendingChanges: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_APP_PROFILES]: createFetchHandler(section, '/appprofile'),
    [FETCH_APP_PROFILE_SCHEMA]: createFetchSchemaHandler(section, '/appprofile/schema'),
    [SAVE_APP_PROFILE]: createSaveProviderHandler(section, '/appprofile'),
    [DELETE_APP_PROFILE]: createRemoveItemHandler(section, '/appprofile')
  },

  //
  // Reducers

  reducers: {
    [SET_APP_PROFILE_VALUE]: createSetSettingValueReducer(section),

    [CLONE_APP_PROFILE]: function(state, { payload }) {
      const id = payload.id;
      const newState = getSectionState(state, section);
      const item = newState.items.find((i) => i.id === id);
      const pendingChanges = { ...item, id: 0 };
      delete pendingChanges.id;

      pendingChanges.name = translate('DefaultNameCopiedProfile', { name: pendingChanges.name });
      newState.pendingChanges = pendingChanges;

      return updateSectionState(state, section, newState);
    }
  }

};
