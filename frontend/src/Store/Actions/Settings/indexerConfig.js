import { createAction } from 'redux-actions';
import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import createSaveHandler from 'Store/Actions/Creators/createSaveHandler';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.indexerConfig';

//
// Actions Types

export const FETCH_INDEXER_CONFIG = 'settings/indexerConfig/fetchIndexerConfig';
export const SET_INDEXER_CONFIG_VALUE = 'SET_INDEXER_CONFIG_VALUE';
export const SAVE_INDEXER_CONFIG = 'SAVE_INDEXER_CONFIG';

//
// Action Creators

export const fetchIndexerConfig = createThunk(FETCH_INDEXER_CONFIG);
export const saveIndexerConfig = createThunk(SAVE_INDEXER_CONFIG);
export const setIndexerConfigValue = createAction(SET_INDEXER_CONFIG_VALUE, (payload) => {
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
    [FETCH_INDEXER_CONFIG]: createFetchHandler(section, '/config/indexer'),
    [SAVE_INDEXER_CONFIG]: createSaveHandler(section, '/config/indexer')
  },

  //
  // Reducers

  reducers: {
    [SET_INDEXER_CONFIG_VALUE]: createSetSettingValueReducer(section)
  }

};
