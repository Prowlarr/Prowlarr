import { createAction } from 'redux-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';
import { filterPredicates, filters, sortPredicates } from './indexerActions';

//
// Variables

export const section = 'indexerIndex';

//
// State

export const defaultState = {
  isSaving: false,
  saveError: null,
  isDeleting: false,
  deleteError: null,
  sortKey: 'sortName',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'sortName',
  secondarySortDirection: sortDirections.ASCENDING,

  tableOptions: {
    showSearchAction: false
  },

  columns: [
    {
      name: 'status',
      columnLabel: () => translate('IndexerStatus'),
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'id',
      columnLabel: () => translate('IndexerId'),
      label: () => translate('Id'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'sortName',
      label: () => translate('IndexerName'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'protocol',
      label: () => translate('Protocol'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'privacy',
      label: () => translate('Privacy'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'priority',
      label: () => translate('Priority'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'appProfileId',
      label: () => translate('SyncProfile'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'added',
      label: () => translate('Added'),
      isSortable: true,
      isVisible: true
    },
    {
      name: 'vipExpiration',
      label: () => translate('VipExpiration'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'capabilities',
      label: () => translate('Categories'),
      isSortable: false,
      isVisible: true
    },
    {
      name: 'minimumSeeders',
      label: () => translate('MinimumSeeders'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'seedRatio',
      label: () => translate('SeedRatio'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'seedTime',
      label: () => translate('SeedTime'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'packSeedTime',
      label: () => translate('PackSeedTime'),
      isSortable: true,
      isVisible: false
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      isSortable: false,
      isVisible: false
    },
    {
      name: 'actions',
      columnLabel: () => translate('Actions'),
      isVisible: true,
      isModifiable: false
    }
  ],

  sortPredicates: {
    ...sortPredicates
  },

  selectedFilterKey: 'all',

  filters,
  filterPredicates,

  filterBuilderProps: [
    {
      name: 'name',
      label: () => translate('IndexerName'),
      type: filterBuilderTypes.STRING
    },
    {
      name: 'enable',
      label: () => translate('Enabled'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'added',
      label: () => translate('Added'),
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'vipExpiration',
      label: () => translate('VipExpiration'),
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'priority',
      label: () => translate('Priority'),
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'protocol',
      label: () => translate('Protocol'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.PROTOCOL
    },
    {
      name: 'privacy',
      label: () => translate('Privacy'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.PRIVACY
    },
    {
      name: 'appProfileId',
      label: () => translate('SyncProfile'),
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.APP_PROFILE
    },
    {
      name: 'tags',
      label: () => translate('Tags'),
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ]
};

export const persistState = [
  'indexerIndex.sortKey',
  'indexerIndex.sortDirection',
  'indexerIndex.selectedFilterKey',
  'indexerIndex.customFilters',
  'indexerIndex.view',
  'indexerIndex.columns',
  'indexerIndex.tableOptions'
];

//
// Actions Types

export const SET_INDEXER_SORT = 'indexerIndex/setIndexerSort';
export const SET_INDEXER_FILTER = 'indexerIndex/setIndexerFilter';
export const SET_INDEXER_VIEW = 'indexerIndex/setIndexerView';
export const SET_INDEXER_TABLE_OPTION = 'indexerIndex/setIndexerTableOption';

//
// Action Creators

export const setIndexerSort = createAction(SET_INDEXER_SORT);
export const setIndexerFilter = createAction(SET_INDEXER_FILTER);
export const setIndexerView = createAction(SET_INDEXER_VIEW);
export const setIndexerTableOption = createAction(SET_INDEXER_TABLE_OPTION);

//
// Reducers

export const reducers = createHandleActions({

  [SET_INDEXER_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_INDEXER_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_INDEXER_VIEW]: function(state, { payload }) {
    return Object.assign({}, state, { view: payload.view });
  },

  [SET_INDEXER_TABLE_OPTION]: createSetTableOptionReducer(section)

}, defaultState, section);
