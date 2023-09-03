import * as app from './appActions';
import * as captcha from './captchaActions';
import * as commands from './commandActions';
import * as customFilters from './customFilterActions';
import * as history from './historyActions';
import * as indexers from './indexerActions';
import * as indexerHistory from './indexerHistoryActions';
import * as indexerIndex from './indexerIndexActions';
import * as indexerStats from './indexerStatsActions';
import * as indexerStatus from './indexerStatusActions';
import * as localization from './localizationActions';
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
  localization,
  indexers,
  indexerHistory,
  indexerIndex,
  indexerStats,
  indexerStatus,
  settings,
  system,
  tags
];
