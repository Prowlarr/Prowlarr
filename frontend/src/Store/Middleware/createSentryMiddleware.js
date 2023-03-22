import * as sentry from '@sentry/browser';
import * as Integrations from '@sentry/integrations';
import { cloneDeep } from 'lodash-es';
import parseUrl from 'Utilities/String/parseUrl';

function cleanseUrl(url) {
  const properties = parseUrl(url);

  return `${properties.pathname}${properties.search}`;
}

function cleanseData(data) {
  const result = cloneDeep(data);

  result.transaction = cleanseUrl(result.transaction);

  if (result.exception) {
    result.exception.values.forEach((exception) => {
      const stacktrace = exception.stacktrace;

      if (stacktrace) {
        stacktrace.frames.forEach((frame) => {
          frame.filename = cleanseUrl(frame.filename);
        });
      }
    });
  }

  result.request.url = cleanseUrl(result.request.url);

  return result;
}

function identity(stuff) {
  return stuff;
}

function stripUrlBase(frame) {
  if (frame.filename && window.Prowlarr.urlBase) {
    frame.filename = frame.filename.replace(window.Prowlarr.urlBase, '');
  }
  return frame;
}

function createMiddleware() {
  return (store) => (next) => (action) => {
    try {
      // Adds a breadcrumb for reporting later (if necessary).
      sentry.addBreadcrumb({
        category: 'redux',
        message: action.type
      });

      return next(action);
    } catch (err) {
      console.error(`[sentry] Reporting error to Sentry: ${err}`);

      // Send the report including breadcrumbs.
      sentry.captureException(err, {
        extra: {
          action: identity(action),
          state: identity(store.getState())
        }
      });
    }
  };
}

export default function createSentryMiddleware() {
  const {
    analytics,
    branch,
    version,
    release,
    userHash,
    isProduction
  } = window.Prowlarr;

  if (!analytics) {
    return;
  }

  const dsn = isProduction ? 'https://b233094711fe4430a0b0c5da2e01df93@sentry.servarr.com/28' :
    'https://116efebd253a4dff9df9475a31510001@sentry.servarr.com/37';

  sentry.init({
    dsn,
    environment: branch,
    release,
    sendDefaultPii: true,
    beforeSend: cleanseData,
    integrations: [
      new Integrations.RewriteFrames({ iteratee: stripUrlBase }),
      new Integrations.Dedupe()
    ]
  });

  sentry.configureScope((scope) => {
    scope.setUser({ username: userHash });
    scope.setTag('version', version);
    scope.setTag('production', isProduction);
  });

  return createMiddleware();
}
