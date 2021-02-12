import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'indexerStatus';

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  items: [],

  details: {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: []
  }
};

//
// Actions Types

export const FETCH_INDEXER_STATUS = 'indexerStatus/fetchIndexerStatus';

//
// Action Creators

export const fetchIndexerStatus = createThunk(FETCH_INDEXER_STATUS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_INDEXER_STATUS]: createFetchHandler(section, '/indexerStatus')
});

//
// Reducers
export const reducers = createHandleActions({}, defaultState, section);
