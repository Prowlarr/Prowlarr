import { createAction } from 'redux-actions';
import { handleThunks } from 'Store/thunks';
import createHandleActions from './Creators/createHandleActions';
import applications from './Settings/applications';
import appProfiles from './Settings/appProfiles';
import development from './Settings/development';
import downloadClients from './Settings/downloadClients';
import general from './Settings/general';
import indexerCategories from './Settings/indexerCategories';
import languages from './Settings/languages';
import notifications from './Settings/notifications';
import ui from './Settings/ui';

export * from './Settings/downloadClients';
export * from './Settings/general';
export * from './Settings/indexerCategories';
export * from './Settings/languages';
export * from './Settings/notifications';
export * from './Settings/applications';
export * from './Settings/appProfiles';
export * from './Settings/development';
export * from './Settings/ui';

//
// Variables

export const section = 'settings';

//
// State

export const defaultState = {
  advancedSettings: false,

  downloadClients: downloadClients.defaultState,
  general: general.defaultState,
  indexerCategories: indexerCategories.defaultState,
  languages: languages.defaultState,
  notifications: notifications.defaultState,
  applications: applications.defaultState,
  appProfiles: appProfiles.defaultState,
  development: development.defaultState,
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
  ...downloadClients.actionHandlers,
  ...general.actionHandlers,
  ...indexerCategories.actionHandlers,
  ...languages.actionHandlers,
  ...notifications.actionHandlers,
  ...applications.actionHandlers,
  ...appProfiles.actionHandlers,
  ...development.actionHandlers,
  ...ui.actionHandlers
});

//
// Reducers

export const reducers = createHandleActions({

  [TOGGLE_ADVANCED_SETTINGS]: (state, { payload }) => {
    return Object.assign({}, state, { advancedSettings: !state.advancedSettings });
  },

  ...downloadClients.reducers,
  ...general.reducers,
  ...indexerCategories.reducers,
  ...languages.reducers,
  ...notifications.reducers,
  ...applications.reducers,
  ...appProfiles.reducers,
  ...development.reducers,
  ...ui.reducers

}, defaultState, section);
