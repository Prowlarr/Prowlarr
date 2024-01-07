import moment from 'moment';
import { batchActions } from 'redux-batched-actions';
import { filterBuilderTypes, filterBuilderValueTypes } from 'Helpers/Props';
import { createThunk, handleThunks } from 'Store/thunks';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import findSelectedFilters from 'Utilities/Filter/findSelectedFilters';
import translate from 'Utilities/String/translate';
import { set, update } from './baseActions';
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
  item: {},
  start: null,
  end: null,

  details: {
    isFetching: false,
    isPopulated: false,
    error: null,
    item: []
  },

  filters: [
    {
      key: 'all',
      label: () => translate('All'),
      filters: []
    },
    {
      key: 'lastSeven',
      label: 'Last 7 Days',
      filters: []
    },
    {
      key: 'lastThirty',
      label: 'Last 30 Days',
      filters: []
    },
    {
      key: 'lastNinety',
      label: 'Last 90 Days',
      filters: []
    }
  ],

  filterBuilderProps: [
    {
      name: 'indexers',
      label: () => translate('Indexers'),
      type: filterBuilderTypes.CONTAINS,
      valueType: filterBuilderValueTypes.INDEXER
    },
    {
      name: 'protocols',
      label: () => translate('Protocols'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.PROTOCOL
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      type: filterBuilderTypes.CONTAINS,
      valueType: filterBuilderValueTypes.TAG
    }
  ],

  selectedFilterKey: 'all'

};

export const persistState = [
  'indexerStats.customFilters',
  'indexerStats.selectedFilterKey'
];

//
// Actions Types

export const FETCH_INDEXER_STATS = 'indexerStats/fetchIndexerStats';
export const SET_INDEXER_STATS_FILTER = 'indexerStats/setIndexerStatsFilter';

function getCustomFilters(state, type) {
  return state.customFilters.items.filter((customFilter) => customFilter.type === type);
}

//
// Action Creators

export const fetchIndexerStats = createThunk(FETCH_INDEXER_STATS);
export const setIndexerStatsFilter = createThunk(SET_INDEXER_STATS_FILTER);

//
// Action Handlers

export const actionHandlers = handleThunks({
  [FETCH_INDEXER_STATS]: function(getState, payload, dispatch) {
    const state = getState();
    const indexerStats = state.indexerStats;
    const customFilters = getCustomFilters(state, section);
    const selectedFilters = findSelectedFilters(indexerStats.selectedFilterKey, indexerStats.filters, customFilters);

    const requestParams = {
      endDate: moment().toISOString()
    };

    selectedFilters.forEach((selectedFilter) => {
      if (selectedFilter.key === 'indexers') {
        requestParams.indexers = selectedFilter.value.join(',');
      }

      if (selectedFilter.key === 'protocols') {
        requestParams.protocols = selectedFilter.value.join(',');
      }

      if (selectedFilter.key === 'tags') {
        requestParams.tags = selectedFilter.value.join(',');
      }
    });

    if (indexerStats.selectedFilterKey !== 'all') {
      if (indexerStats.selectedFilterKey === 'lastSeven') {
        requestParams.startDate = moment().add(-7, 'days').endOf('day').toISOString();
      }

      if (indexerStats.selectedFilterKey === 'lastThirty') {
        requestParams.startDate = moment().add(-30, 'days').endOf('day').toISOString();
      }

      if (indexerStats.selectedFilterKey === 'lastNinety') {
        requestParams.startDate = moment().add(-90, 'days').endOf('day').toISOString();
      }
    }

    const basesAttrs = {
      section,
      isFetching: true
    };

    const attrs = basesAttrs;

    dispatch(set(attrs));

    const promise = createAjaxRequest({
      url: '/indexerStats',
      data: requestParams
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
  },

  [SET_INDEXER_STATS_FILTER]: function(getState, payload, dispatch) {
    dispatch(set({ section, ...payload }));
    dispatch(fetchIndexerStats());
  }
});

//
// Reducers
export const reducers = createHandleActions({}, defaultState, section);
