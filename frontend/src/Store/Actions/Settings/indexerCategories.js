import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import { createThunk } from 'Store/thunks';

//
// Variables

const section = 'settings.indexerCategories';

//
// Actions Types

export const FETCH_INDEXER_CATEGORIES = 'settings/indexerFlags/fetchIndexerCategories';

//
// Action Creators

export const fetchIndexerCategories = createThunk(FETCH_INDEXER_CATEGORIES);

//
// Details

export default {

  //
  // State

  defaultState: {
    isFetching: false,
    isPopulated: false,
    error: null,
    items: []
  },

  //
  // Action Handlers

  actionHandlers: {
    [FETCH_INDEXER_CATEGORIES]: createFetchHandler(section, '/indexer/categories')
  },

  //
  // Reducers

  reducers: {

  }

};
