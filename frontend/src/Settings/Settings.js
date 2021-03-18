import React from 'react';
import Link from 'Components/Link/Link';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import translate from 'Utilities/String/translate';
import SettingsToolbarConnector from './SettingsToolbarConnector';
import styles from './Settings.css';

function Settings() {
  return (
    <PageContent title={translate('Settings')}>
      <SettingsToolbarConnector
        hasPendingChanges={false}
      />

      <PageContentBody>
        <Link
          className={styles.link}
          to="/settings/applications"
        >
          Applications
        </Link>

        <div className={styles.summary}>
          Applications and settings to configure how prowlarr interacts with your PVR programs
        </div>

        <Link
          className={styles.link}
          to="/settings/downloadclients"
        >
          {translate('DownloadClients')}
        </Link>

        <div className={styles.summary}>
          {translate('DownloadClientsSettingsSummary')}
        </div>

        <Link
          className={styles.link}
          to="/settings/connect"
        >
          Notifications
        </Link>

        <div className={styles.summary}>
          {translate('ConnectSettingsSummary')}
        </div>

        <Link
          className={styles.link}
          to="/settings/tags"
        >
          {translate('Tags')}
        </Link>

        <div className={styles.summary}>
          {translate('TagsSettingsSummary')}
        </div>

        <Link
          className={styles.link}
          to="/settings/general"
        >
          {translate('General')}
        </Link>

        <div className={styles.summary}>
          {translate('GeneralSettingsSummary')}
        </div>

        <Link
          className={styles.link}
          to="/settings/ui"
        >
          {translate('UI')}
        </Link>

        <div className={styles.summary}>
          {translate('UISettingsSummary')}
        </div>
      </PageContentBody>
    </PageContent>
  );
}

Settings.propTypes = {
};

export default Settings;
