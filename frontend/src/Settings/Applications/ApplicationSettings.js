import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import { icons } from 'Helpers/Props';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';
import ApplicationsConnector from './Applications/ApplicationsConnector';

class ApplicationSettings extends Component {
  render() {
    const {
      isTestingAll,
      dispatchTestAllApplications
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
              />

              <PageToolbarButton
                label={translate('TestAllApps')}
                iconName={icons.TEST}
                isSpinning={isTestingAll}
                onPress={dispatchTestAllApplications}
              />
            </Fragment>
          }
        />

        <PageContentBody>
          <ApplicationsConnector />
        </PageContentBody>
      </PageContent>
    );
  }
}

ApplicationSettings.propTypes = {
  isTestingAll: PropTypes.bool.isRequired,
  dispatchTestAllApplications: PropTypes.func.isRequired
};

export default ApplicationSettings;
