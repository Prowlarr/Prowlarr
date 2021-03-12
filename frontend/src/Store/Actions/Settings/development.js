import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.development';

//
// Actions Types

export const FETCH_DEVELOPMENT_SETTINGS = 'settings/development/fetchDevelopmentSettings';
export const SET_DEVELOPMENT_SETTINGS_VALUE = 'settings/development/setDevelopmentSettingsValue';
export const SAVE_DEVELOPMENT_SETTINGS = 'settings/development/saveDevelopmentSettings';

//
// Action Creators

export const fetchDevelopmentSettings = createThunk(FETCH_DEVELOPMENT_SETTINGS);
export const saveDevelopmentSettings = createThunk(SAVE_DEVELOPMENT_SETTINGS);
export const setDevelopmentSettingsValue = createAction(SET_DEVELOPMENT_SETTINGS_VALUE, (payload) => {
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
    pendingChanges: {},
    isSaving: false,
    saveError: null,
    item: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_DEVELOPMENT_SETTINGS]: createFetchHandler(section, '/config/development'),
    [SAVE_DEVELOPMENT_SETTINGS]: createSaveHandler(section, '/config/development')
  },

  //
  // Reducers

  reducers: {
    [SET_DEVELOPMENT_SETTINGS_VALUE]: createSetSettingValueReducer(section)
  }

};
