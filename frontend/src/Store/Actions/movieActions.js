import _ from 'lodash';
import { createAction } from 'redux-actions';
import { sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
// import { batchActions } from 'redux-batched-actions';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import dateFilterPredicate from 'Utilities/Date/dateFilterPredicate';
import translate from 'Utilities/String/translate';
import { updateItem } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createRemoveItemHandler from './Creators/createRemoveItemHandler';
import createSaveProviderHandler from './Creators/createSaveProviderHandler';
import createSetSettingValueReducer from './Creators/Reducers/createSetSettingValueReducer';

//
// Variables

export const section = 'movies';

export const filters = [
  {
    key: 'all',
    label: translate('All'),
    filters: []
  }
];

export const filterPredicates = {
  added: function(item, filterValue, type) {
    return dateFilterPredicate(item.added, filterValue, type);
  }
};

export const sortPredicates = {
  status: function(item) {
    let result = 0;

    if (item.monitored) {
      result += 4;
    }

    if (item.status === 'announced') {
      result++;
    }

    if (item.status === 'inCinemas') {
      result += 2;
    }

    if (item.status === 'released') {
      result += 3;
    }

    return result;
  }
};

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  error: null,
  isSaving: false,
  saveError: null,
  items: [],
  sortKey: 'name',
  sortDirection: sortDirections.ASCENDING,
  pendingChanges: {}
};

//
// Actions Types

export const FETCH_MOVIES = 'movies/fetchMovies';
export const SET_MOVIE_VALUE = 'movies/setMovieValue';
export const SAVE_MOVIE = 'movies/saveMovie';
export const DELETE_MOVIE = 'movies/deleteMovie';

export const TOGGLE_MOVIE_MONITORED = 'movies/toggleMovieMonitored';

//
// Action Creators

export const fetchMovies = createThunk(FETCH_MOVIES);
export const saveMovie = createThunk(SAVE_MOVIE, (payload) => {
  const newPayload = {
    ...payload
  };

  if (payload.moveFiles) {
    newPayload.queryParams = {
      moveFiles: true
    };
  }

  delete newPayload.moveFiles;

  return newPayload;
});

export const deleteMovie = createThunk(DELETE_MOVIE, (payload) => {
  return {
    ...payload,
    queryParams: {
      deleteFiles: payload.deleteFiles
    }
  };
});

export const toggleMovieMonitored = createThunk(TOGGLE_MOVIE_MONITORED);

export const setMovieValue = createAction(SET_MOVIE_VALUE, (payload) => {
  return {
    section,
    ...payload
  };
});

//
// Helpers

function getSaveAjaxOptions({ ajaxOptions, payload }) {
  if (payload.moveFolder) {
    ajaxOptions.url = `${ajaxOptions.url}?moveFolder=true`;
  }

  return ajaxOptions;
}

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_MOVIES]: createFetchHandler(section, '/movie'),
  [SAVE_MOVIE]: createSaveProviderHandler(section, '/movie', { getAjaxOptions: getSaveAjaxOptions }),
  [DELETE_MOVIE]: createRemoveItemHandler(section, '/movie'),

  [TOGGLE_MOVIE_MONITORED]: (getState, payload, dispatch) => {
    const {
      movieId: id,
      monitored
    } = payload;

    const movie = _.find(getState().movies.items, { id });

    dispatch(updateItem({
      id,
      section,
      isSaving: true
    }));

    const promise = createAjaxRequest({
      url: `/movie/${id}`,
      method: 'PUT',
      data: JSON.stringify({
        ...movie,
        monitored
      }),
      dataType: 'json'
    }).request;

    promise.done((data) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false,
        monitored
      }));
    });

    promise.fail((xhr) => {
      dispatch(updateItem({
        id,
        section,
        isSaving: false
      }));
    });
  }

});

//
// Reducers

export const reducers = createHandleActions({

  [SET_MOVIE_VALUE]: createSetSettingValueReducer(section)

}, defaultState, section);
