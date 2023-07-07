import $ from 'jquery';
import { createAction } from 'redux-actions';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getSectionState from 'Utilities/State/getSectionState';
import updateSectionState from 'Utilities/State/updateSectionState';
import translate from 'Utilities/String/translate';
import { set } from './baseActions';
import createFetchHandler from './Creators/createFetchHandler';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'releases';

let abortCurrentRequest = null;

//
// State

export const defaultState = {
  isFetching: false,
  isPopulated: false,
  isGrabbing: false,
  error: null,
  grabError: null,
  items: [],
  sortKey: 'age',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortTitle',
  secondarySortDirection: sortDirections.ASCENDING,

  defaults: {
    searchType: 'search',
    searchQuery: '',
    searchIndexerIds: [],
    searchCategories: [],
    searchLimit: 100,
    searchOffset: 0
  },

  columns: [
    {
      name: 'select',
      columnLabel: 'Select',
      isSortable: false,
      isVisible: true,
      isModifiable: false,
      isHidden: true
    },
    {
      name: 'protocol',
      label: translate('Protocol'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'age',
      label: translate('Age'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'sortTitle',
      label: translate('Title'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'indexer',
      label: translate('Indexer'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'size',
      label: translate('Size'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'files',
      label: translate('Files'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'grabs',
      label: translate('Grabs'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'peers',
      label: translate('Peers'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'category',
      label: translate('Category'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'indexerFlags',
      columnLabel: 'Indexer Flags',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'actions',
      columnLabel: translate('Actions'),
      isVisible: true,
      isModifiable: false
    }
  ],

  sortPredicates: {
    age: function(item) {
      return item.ageMinutes;
    },

    peers: function(item) {
      const seeders = item.seeders || 0;
      const leechers = item.leechers || 0;

      return seeders * 1000000 + leechers;
    },

    indexerFlags: function(item) {
      const indexerFlags = item.indexerFlags;
      const releaseWeight = item.releaseWeight;

      if (indexerFlags.length === 0) {
        return releaseWeight + 1000000;
      }

      return releaseWeight;
    },

    category: function(item) {
      if (item.categories !== undefined && item.categories.length > 0) {
        const sortedCats = item.categories.filter((cat) => cat.name !== undefined).sort((c) => c.id);
        const firstCat = sortedCats[0];

        return firstCat.name;
      }
    }
  },

  filters: [
    {
      key: 'all',
      label: translate('All'),
      filters: []
    }
  ],

  filterBuilderProps: [
    {
      name: 'title',
      label: translate('Title'),
      type: filterBuilderTypes.STRING
    },
    {
      name: 'age',
      label: translate('Age'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'protocol',
      label: translate('Protocol'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.PROTOCOL
    },
    {
      name: 'indexerId',
      label: translate('Indexer'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.INDEXER
    },
    {
      name: 'size',
      label: translate('Size'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'files',
      label: translate('Files'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'grabs',
      label: translate('Grabs'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'seeders',
      label: translate('Seeders'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'peers',
      label: translate('Peers'),
      type: filterBuilderTypes.NUMBER
    }
  ],
  selectedFilterKey: 'all'

};

export const persistState = [
  'releases.sortKey',
  'releases.sortDirection',
  'releases.customFilters',
  'releases.selectedFilterKey',
  'releases.columns'
];

//
// Actions Types

export const FETCH_RELEASES = 'releases/fetchReleases';
export const CANCEL_FETCH_RELEASES = 'releases/cancelFetchReleases';
export const SET_RELEASES_SORT = 'releases/setReleasesSort';
export const CLEAR_RELEASES = 'releases/clearReleases';
export const GRAB_RELEASE = 'releases/grabRelease';
export const SAVE_RELEASE = 'releases/saveRelease';
export const BULK_GRAB_RELEASES = 'release/bulkGrabReleases';
export const UPDATE_RELEASE = 'releases/updateRelease';
export const SET_RELEASES_FILTER = 'releases/setReleasesFilter';
export const SET_RELEASES_TABLE_OPTION = 'releases/setReleasesTableOption';
export const SET_SEARCH_DEFAULT = 'releases/setSearchDefault';

//
// Action Creators

export const fetchReleases = createThunk(FETCH_RELEASES);
export const cancelFetchReleases = createThunk(CANCEL_FETCH_RELEASES);
export const setReleasesSort = createAction(SET_RELEASES_SORT);
export const clearReleases = createAction(CLEAR_RELEASES);
export const grabRelease = createThunk(GRAB_RELEASE);
export const saveRelease = createThunk(SAVE_RELEASE);
export const bulkGrabReleases = createThunk(BULK_GRAB_RELEASES);
export const updateRelease = createAction(UPDATE_RELEASE);
export const setReleasesFilter = createAction(SET_RELEASES_FILTER);
export const setReleasesTableOption = createAction(SET_RELEASES_TABLE_OPTION);
export const setSearchDefault = createAction(SET_SEARCH_DEFAULT);

//
// Helpers

const fetchReleasesHelper = createFetchHandler(section, '/search');

//
// Action Handlers

export const actionHandlers = handleThunks({

  [FETCH_RELEASES]: function(getState, payload, dispatch) {
    const abortRequest = fetchReleasesHelper(getState, payload, dispatch);

    abortCurrentRequest = abortRequest;
  },

  [CANCEL_FETCH_RELEASES]: function(getState, payload, dispatch) {
    if (abortCurrentRequest) {
      abortCurrentRequest = abortCurrentRequest();
    }
  },

  [GRAB_RELEASE]: function(getState, payload, dispatch) {
    const guid = payload.guid;

    dispatch(updateRelease({ guid, isGrabbing: true }));

    const promise = createAjaxRequest({
      url: '/search',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(payload)
    }).request;

    promise.done((data) => {
      dispatch(updateRelease({
        guid,
        isGrabbing: false,
        isGrabbed: true,
        grabError: null
      }));
    });

    promise.fail((xhr) => {
      const grabError = xhr.responseJSON && xhr.responseJSON.message || 'Failed to add to download queue';

      dispatch(updateRelease({
        guid,
        isGrabbing: false,
        isGrabbed: false,
        grabError
      }));
    });
  },

  [SAVE_RELEASE]: function(getState, payload, dispatch) {
    const link = payload.downloadUrl;
    const file = payload.fileName;

    $.ajax({
      url: link,
      method: 'GET',
      headers: {
        'X-Prowlarr-Client': true
      },
      xhrFields: {
        responseType: 'blob'
      },
      success: function(data) {
        const a = document.createElement('a');
        const url = window.URL.createObjectURL(data);
        a.href = url;
        a.download = file;
        document.body.append(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(url);
      }
    });
  },

  [BULK_GRAB_RELEASES]: function(getState, payload, dispatch) {
    dispatch(set({
      section,
      isGrabbing: true
    }));

    const promise = createAjaxRequest({
      url: '/search/bulk',
      method: 'POST',
      contentType: 'application/json',
      data: JSON.stringify(payload)
    }).request;

    promise.done((data) => {
      dispatch(batchActions([
        ...data.map((release) => {
          return updateRelease({
            isGrabbing: false,
            isGrabbed: true,
            grabError: null
          });
        }),

        set({
          section,
          isGrabbing: false
        })
      ]));
    });

    promise.fail((xhr) => {
      dispatch(set({
        section,
        isGrabbing: false,
        grabError: xhr
      }));
    });
  }
});

//
// Reducers

export const reducers = createHandleActions({

  [CLEAR_RELEASES]: (state) => {
    return Object.assign({}, state, defaultState);
  },

  [UPDATE_RELEASE]: (state, { payload }) => {
    const guid = payload.guid;
    const newState = Object.assign({}, state);
    const items = newState.items;
    const index = items.findIndex((item) => item.guid === guid);

    // Don't try to update if there isnt a matching item (the user closed the modal)
    if (index >= 0) {
      const item = Object.assign({}, items[index], payload);

      newState.items = [...items];
      newState.items.splice(index, 1, item);
    }

    return newState;
  },

  [SET_SEARCH_DEFAULT]: function(state, { payload }) {
    const newState = getSectionState(state, section);

    newState.defaults = {
      ...newState.defaults,
      ...payload
    };

    return updateSectionState(state, section, newState);
  },

  [SET_RELEASES_FILTER]: createSetClientSideCollectionFilterReducer(section),
  [SET_RELEASES_SORT]: createSetClientSideCollectionSortReducer(section),

  [SET_RELEASES_TABLE_OPTION]: createSetTableOptionReducer(section)

}, defaultState, section);
