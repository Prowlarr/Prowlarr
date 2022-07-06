import createFetchHandler from 'Store/Actions/Creators/createFetchHandler';
import { createThunk, handleThunks } from 'Store/thunks';
import createHandleActions from './Creators/createHandleActions';

//
// Variables

export const section = 'localization';

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

export const FETCH_LOCALIZATION_OPTIONS = 'localization/fetchLocalizationOptions';

//
// Action Creators

export const fetchLocalizationOptions = createThunk(FETCH_LOCALIZATION_OPTIONS);

//
// Action Handlers
export const actionHandlers = handleThunks({

  [FETCH_LOCALIZATION_OPTIONS]: createFetchHandler(section, '/localization/options')
});

//
// Reducers
export const reducers = createHandleActions({}, defaultState, section);
