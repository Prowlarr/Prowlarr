import React from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';
import ApplicationsConnector from './Applications/ApplicationsConnector';

function ApplicationSettings() {
  return (
    <PageContent title={translate('Applications')}>
      <SettingsToolbarConnector
        showSave={false}
      />

      <PageContentBody>
        <ApplicationsConnector />
      </PageContentBody>
    </PageContent>
  );
}

export default ApplicationSettings;
