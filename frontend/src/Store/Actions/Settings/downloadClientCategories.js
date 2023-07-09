import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import createClearReducer from 'Store/Actions/Creators/Reducers/createClearReducer';
import createSetProviderFieldValueReducer from 'Store/Actions/Creators/Reducers/createSetProviderFieldValueReducer';
import createSetSettingValueReducer from 'Store/Actions/Creators/Reducers/createSetSettingValueReducer';
import { createThunk } from 'Store/thunks';
import getNextId from 'Utilities/State/getNextId';
import getProviderState from 'Utilities/State/getProviderState';
import getSectionState from 'Utilities/State/getSectionState';
import selectProviderSchema from 'Utilities/State/selectProviderSchema';
import { removeItem, set, update, updateItem } from '../baseActions';

//
// Variables

const section = 'settings.downloadClientCategories';

//
// Actions Types

export const FETCH_DOWNLOAD_CLIENT_CATEGORIES = 'settings/downloadClientCategories/fetchDownloadClientCategories';
export const FETCH_DOWNLOAD_CLIENT_CATEGORY_SCHEMA = 'settings/downloadClientCategories/fetchDownloadClientCategorySchema';
export const SELECT_DOWNLOAD_CLIENT_CATEGORY_SCHEMA = 'settings/downloadClientCategories/selectDownloadClientCategorySchema';
export const SET_DOWNLOAD_CLIENT_CATEGORY_VALUE = 'settings/downloadClientCategories/setDownloadClientCategoryValue';
export const SET_DOWNLOAD_CLIENT_CATEGORY_FIELD_VALUE = 'settings/downloadClientCategories/setDownloadClientCategoryFieldValue';
export const SAVE_DOWNLOAD_CLIENT_CATEGORY = 'settings/downloadClientCategories/saveDownloadClientCategory';
export const DELETE_DOWNLOAD_CLIENT_CATEGORY = 'settings/downloadClientCategories/deleteDownloadClientCategory';
export const DELETE_ALL_DOWNLOAD_CLIENT_CATEGORY = 'settings/downloadClientCategories/deleteAllDownloadClientCategory';
export const CLEAR_DOWNLOAD_CLIENT_CATEGORIES = 'settings/downloadClientCategories/clearDownloadClientCategories';
export const CLEAR_DOWNLOAD_CLIENT_CATEGORY_PENDING = 'settings/downloadClientCategories/clearDownloadClientCategoryPending';
//
// Action Creators

export const fetchDownloadClientCategories = createThunk(FETCH_DOWNLOAD_CLIENT_CATEGORIES);
export const fetchDownloadClientCategorySchema = createThunk(FETCH_DOWNLOAD_CLIENT_CATEGORY_SCHEMA);
export const selectDownloadClientCategorySchema = createAction(SELECT_DOWNLOAD_CLIENT_CATEGORY_SCHEMA);

export const saveDownloadClientCategory = createThunk(SAVE_DOWNLOAD_CLIENT_CATEGORY);
export const deleteDownloadClientCategory = createThunk(DELETE_DOWNLOAD_CLIENT_CATEGORY);
export const deleteAllDownloadClientCategory = createThunk(DELETE_ALL_DOWNLOAD_CLIENT_CATEGORY);

export const setDownloadClientCategoryValue = createAction(SET_DOWNLOAD_CLIENT_CATEGORY_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const setDownloadClientCategoryFieldValue = createAction(SET_DOWNLOAD_CLIENT_CATEGORY_FIELD_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

export const clearDownloadClientCategory = createAction(CLEAR_DOWNLOAD_CLIENT_CATEGORIES);

export const clearDownloadClientCategoryPending = createThunk(CLEAR_DOWNLOAD_CLIENT_CATEGORY_PENDING);

//
// Details

export default {

  //
  // State

  defaultState: {
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
    items: [],
    pendingChanges: {}
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_DOWNLOAD_CLIENT_CATEGORIES]: (getState, payload, dispatch) => {
      let tags = [];
      if (payload.id) {
        const cfState = getSectionState(getState(), 'settings.downloadClients', true);
        const cf = cfState.items[cfState.itemMap[payload.id]];
        tags = cf.categories.map((tag, i) => {
          return {
            id: i + 1,
            ...tag
          };
        });
      }

      dispatch(batchActions([
        update({ section, data: tags }),
        set({
          section,
          isPopulated: true
        })
      ]));
    },

    [SAVE_DOWNLOAD_CLIENT_CATEGORY]: (getState, payload, dispatch) => {
      const {
        id,
        ...otherPayload
      } = payload;

      const saveData = getProviderState({ id, ...otherPayload }, getState, section, false);

      // we have to set id since not actually posting to server yet
      if (!saveData.id) {
        saveData.id = getNextId(getState().settings.downloadClientCategories.items);
      }

      dispatch(batchActions([
        updateItem({ section, ...saveData }),
        set({
          section,
          pendingChanges: {}
        })
      ]));
    },

    [DELETE_DOWNLOAD_CLIENT_CATEGORY]: (getState, payload, dispatch) => {
      const id = payload.id;
      return dispatch(removeItem({ section, id }));
    },

    [DELETE_ALL_DOWNLOAD_CLIENT_CATEGORY]: (getState, payload, dispatch) => {
      return dispatch(set({
        section,
        items: []
      }));
    },

    [CLEAR_DOWNLOAD_CLIENT_CATEGORY_PENDING]: (getState, payload, dispatch) => {
      return dispatch(set({
        section,
        pendingChanges: {}
      }));
    }
  },

  //
  // Reducers

  reducers: {
    [SET_DOWNLOAD_CLIENT_CATEGORY_VALUE]: createSetSettingValueReducer(section),
    [SET_DOWNLOAD_CLIENT_CATEGORY_FIELD_VALUE]: createSetProviderFieldValueReducer(section),

    [SELECT_DOWNLOAD_CLIENT_CATEGORY_SCHEMA]: (state, { payload }) => {
      return selectProviderSchema(state, section, payload, (selectedSchema) => {
        return selectedSchema;
      });
    },

    [CLEAR_DOWNLOAD_CLIENT_CATEGORIES]: createClearReducer(section, {
      isPopulated: false,
      error: null,
      items: []
    })
  }
};
