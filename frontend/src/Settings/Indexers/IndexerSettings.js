import React from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';
import IndexerProxiesConnector from './IndexerProxies/IndexerProxiesConnector';

function IndexerSettings() {
  return (
    <PageContent title={translate('Proxies')}>
      <SettingsToolbarConnector
        showSave={false}
      />

      <PageContentBody>
        <IndexerProxiesConnector />
      </PageContentBody>
    </PageContent>
  );
}

export default IndexerSettings;
