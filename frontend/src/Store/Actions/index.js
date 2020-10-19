import * as app from './appActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as history from './historyActions';
import * as movies from './movieActions';
import * as movieIndex from './movieIndexActions';
import * as oAuth from './oAuthActions';
import * as paths from './pathActions';
import * as providerOptions from './providerOptionActions';
import * as releases from './releaseActions';
import * as settings from './settingsActions';
import * as system from './systemActions';
import * as tags from './tagActions';

export default [
  app,
  captcha,
  commands,
  customFilters,
  history,
  oAuth,
  paths,
  providerOptions,
  releases,
  movies,
  movieIndex,
  settings,
  system,
  tags
];
