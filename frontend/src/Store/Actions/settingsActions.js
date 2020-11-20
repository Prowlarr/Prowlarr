import { createAction } from 'redux-actions';
import { handleThunks } from 'Store/thunks';
import createHandleActions from './Creators/createHandleActions';
import applications from './Settings/applications';
import general from './Settings/general';
import indexerCategories from './Settings/indexerCategories';
import indexerFlags from './Settings/indexerFlags';
import indexerOptions from './Settings/indexerOptions';
import languages from './Settings/languages';
import notifications from './Settings/notifications';
import ui from './Settings/ui';

export * from './Settings/general';
export * from './Settings/indexerCategories';
export * from './Settings/indexerFlags';
export * from './Settings/indexerOptions';
export * from './Settings/languages';
export * from './Settings/notifications';
export * from './Settings/applications';
export * from './Settings/ui';

//
// Variables

export const section = 'settings';

//
// State

export const defaultState = {
  advancedSettings: false,

  general: general.defaultState,
  indexerCategories: indexerCategories.defaultState,
  indexerFlags: indexerFlags.defaultState,
  indexerOptions: indexerOptions.defaultState,
  languages: languages.defaultState,
  notifications: notifications.defaultState,
  applications: applications.defaultState,
  ui: ui.defaultState
};

export const persistState = [
  'settings.advancedSettings'
];

//
// Actions Types

export const TOGGLE_ADVANCED_SETTINGS = 'settings/toggleAdvancedSettings';

//
// Action Creators

export const toggleAdvancedSettings = createAction(TOGGLE_ADVANCED_SETTINGS);

//
// Action Handlers

export const actionHandlers = handleThunks({
  ...general.actionHandlers,
  ...indexerCategories.actionHandlers,
  ...indexerFlags.actionHandlers,
  ...indexerOptions.actionHandlers,
  ...languages.actionHandlers,
  ...notifications.actionHandlers,
  ...applications.actionHandlers,
  ...ui.actionHandlers
});

//
// Reducers

export const reducers = createHandleActions({

  [TOGGLE_ADVANCED_SETTINGS]: (state, { payload }) => {
    return Object.assign({}, state, { advancedSettings: !state.advancedSettings });
  },

  ...general.reducers,
  ...indexerCategories.reducers,
  ...indexerFlags.reducers,
  ...indexerOptions.reducers,
  ...languages.reducers,
  ...notifications.reducers,
  ...applications.reducers,
  ...ui.reducers

}, defaultState, section);
