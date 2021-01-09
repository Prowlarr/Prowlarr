import { createThunk, handleThunks } from 'Store/thunks';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'indexerStats';

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

export const FETCH_INDEXER_STATS = 'indexerStats/fetchIndexerStats';

//
// Action Creators

export const fetchIndexerStats = createThunk(FETCH_INDEXER_STATS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_INDEXER_STATS]: createFetchHandler(section, '/indexerStats')
});

//
// Reducers
export const reducers = createHandleActions({}, defaultState, section);
