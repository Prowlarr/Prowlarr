import PropTypes from 'prop-types';
import React from 'react';
import { Redirect, Route } from 'react-router-dom';
import NotFound from 'Components/NotFound';
import Switch from 'Components/Router/Switch';
import HistoryConnector from 'History/HistoryConnector';
import IndexerIndex from 'Indexer/Index/IndexerIndex';
import IndexerStats from 'Indexer/Stats/IndexerStats';
import SearchIndexConnector from 'Search/SearchIndexConnector';
import ApplicationSettingsConnector from 'Settings/Applications/ApplicationSettingsConnector';
import DevelopmentSettingsConnector from 'Settings/Development/DevelopmentSettingsConnector';
import DownloadClientSettingsConnector from 'Settings/DownloadClients/DownloadClientSettingsConnector';
import GeneralSettingsConnector from 'Settings/General/GeneralSettingsConnector';
import IndexerSettings from 'Settings/Indexers/IndexerSettings';
import NotificationSettings from 'Settings/Notifications/NotificationSettings';
import Settings from 'Settings/Settings';
import TagSettings from 'Settings/Tags/TagSettings';
import UISettingsConnector from 'Settings/UI/UISettingsConnector';
import BackupsConnector from 'System/Backup/BackupsConnector';
import LogsTableConnector from 'System/Events/LogsTableConnector';
import Logs from 'System/Logs/Logs';
import Status from 'System/Status/Status';
import Tasks from 'System/Tasks/Tasks';
import UpdatesConnector from 'System/Updates/UpdatesConnector';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';

function AppRoutes(props) {
  const {
    app
  } = props;

  return (
    <Switch>
      {/*
        Indexers
      */}

      <Route
        exact={true}
        path="/"
        component={IndexerIndex}
      />

      {
        window.Prowlarr.urlBase &&
          <Route
            exact={true}
            path="/"
            addUrlBase={false}
            render={() => {
              return (
                <Redirect
                  to={getPathWithUrlBase('/')}
                  component={app}
                />
              );
            }}
          />
      }

      <Route
        path="/indexers/stats"
        component={IndexerStats}
      />

      {/*
        Search
      */}

      <Route
        path="/search"
        component={SearchIndexConnector}
      />

      {/*
        Activity
      */}

      <Route
        path="/history"
        component={HistoryConnector}
      />

      {/*
        Settings
      */}

      <Route
        exact={true}
        path="/settings"
        component={Settings}
      />

      <Route
        path="/settings/indexers"
        component={IndexerSettings}
      />

      <Route
        path="/settings/applications"
        component={ApplicationSettingsConnector}
      />

      <Route
        path="/settings/downloadclients"
        component={DownloadClientSettingsConnector}
      />

      <Route
        path="/settings/connect"
        component={NotificationSettings}
      />

      <Route
        path="/settings/tags"
        component={TagSettings}
      />

      <Route
        path="/settings/general"
        component={GeneralSettingsConnector}
      />

      <Route
        path="/settings/ui"
        component={UISettingsConnector}
      />

      <Route
        path="/settings/development"
        component={DevelopmentSettingsConnector}
      />

      {/*
        System
      */}

      <Route
        path="/system/status"
        component={Status}
      />

      <Route
        path="/system/tasks"
        component={Tasks}
      />

      <Route
        path="/system/backup"
        component={BackupsConnector}
      />

      <Route
        path="/system/updates"
        component={UpdatesConnector}
      />

      <Route
        path="/system/events"
        component={LogsTableConnector}
      />

      <Route
        path="/system/logs/files"
        component={Logs}
      />

      {/*
        Not Found
      */}

      <Route
        path="*"
        component={NotFound}
      />
    </Switch>
  );
}

AppRoutes.propTypes = {
  app: PropTypes.func.isRequired
};

export default AppRoutes;
