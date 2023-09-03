import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import { set, update } from './baseActions';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'indexerHistory';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: []
};

//
// Actions Types

export const FETCH_INDEXER_HISTORY = 'indexerHistory/fetchIndexerHistory';
export const CLEAR_INDEXER_HISTORY = 'indexerHistory/clearIndexerHistory';

//
// Action Creators

export const fetchIndexerHistory = createThunk(FETCH_INDEXER_HISTORY);
export const clearIndexerHistory = createAction(CLEAR_INDEXER_HISTORY);

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_INDEXER_HISTORY]: function(getState, payload, dispatch) {
    dispatch(set({ section, isFetching: true }));

    const promise = createAjaxRequest({
      url: '/history/indexer',
      data: payload
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        update({ section, data }),

        set({
          section,
          isFetching: false,
          isPopulated: true,
          error: null
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isFetching: false,
        isPopulated: false,
        error: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [CLEAR_INDEXER_HISTORY]: (state) => {
    return Object.assign({}, state, defaultState);
  }

}, defaultState, section);
