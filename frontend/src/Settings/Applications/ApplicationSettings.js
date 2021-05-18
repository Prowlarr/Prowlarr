import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import { icons } from 'Helpers/Props';
import AppProfilesConnector from 'Settings/Profiles/App/AppProfilesConnector';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';
import ApplicationsConnector from './Applications/ApplicationsConnector';

class ApplicationSettings extends Component {
  render() {
    const {
      isTestingAll,
      isSyncingIndexers,
      onTestAllPress,
      onAppIndexerSyncPress
    } = this.props;

    return (
      <PageContent title={translate('Applications')}>
        <SettingsToolbarConnector
          showSave={false}
          additionalButtons={
            <Fragment>
              <PageToolbarSeparator />

              <PageToolbarButton
                label={translate('SyncAppIndexers')}
                iconName={icons.REFRESH}
                isSpinning={isSyncingIndexers}
                onPress={onAppIndexerSyncPress}
              />

              <PageToolbarButton
                label={translate('TestAllApps')}
                iconName={icons.TEST}
                isSpinning={isTestingAll}
                onPress={onTestAllPress}
              />
            </Fragment>
          }
        />

        <PageContentBody>
          <ApplicationsConnector />
          <AppProfilesConnector />
        </PageContentBody>
      </PageContent>
    );
  }
}

ApplicationSettings.propTypes = {
  isTestingAll: PropTypes.bool.isRequired,
  isSyncingIndexers: PropTypes.bool.isRequired,
  onTestAllPress: PropTypes.func.isRequired,
  onAppIndexerSyncPress: PropTypes.func.isRequired
};

export default ApplicationSettings;
