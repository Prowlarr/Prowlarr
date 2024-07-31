import React, { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import { APP_INDEXER_SYNC } from 'Commands/commandNames';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import { icons } from 'Helpers/Props';
import AppProfilesConnector from 'Settings/Profiles/App/AppProfilesConnector';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import { executeCommand } from 'Store/Actions/commandActions';
import { testAllApplications } from 'Store/Actions/Settings/applications';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import translate from 'Utilities/String/translate';
import ApplicationsConnector from './Applications/ApplicationsConnector';
import ManageApplicationsModal from './Applications/Manage/ManageApplicationsModal';

function ApplicationSettings() {
  const isSyncingIndexers = useSelector(
    createCommandExecutingSelector(APP_INDEXER_SYNC)
  );
  const isTestingAll = useSelector(
    (state: AppState) => state.settings.applications.isTestingAll
  );
  const dispatch = useDispatch();

  const [isManageApplicationsOpen, setIsManageApplicationsOpen] =
    useState(false);

  const onManageApplicationsPress = useCallback(() => {
    setIsManageApplicationsOpen(true);
  }, [setIsManageApplicationsOpen]);

  const onManageApplicationsModalClose = useCallback(() => {
    setIsManageApplicationsOpen(false);
  }, [setIsManageApplicationsOpen]);

  const onAppIndexerSyncPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: APP_INDEXER_SYNC,
        forceSync: true,
      })
    );
  }, [dispatch]);

  const onTestAllPress = useCallback(() => {
    dispatch(testAllApplications());
  }, [dispatch]);

  return (
    <PageContent title={translate('Applications')}>
      <SettingsToolbarConnector
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
        showSave={false}
        additionalButtons={
          <>
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

            <PageToolbarButton
              label={translate('ManageApplications')}
              iconName={icons.MANAGE}
              onPress={onManageApplicationsPress}
            />
          </>
        }
      />

      <PageContentBody>
        {/* eslint-disable-next-line @typescript-eslint/ban-ts-comment */}
        {/* @ts-ignore */}
        <ApplicationsConnector />
        {/* eslint-disable-next-line @typescript-eslint/ban-ts-comment */}
        {/* @ts-ignore */}
        <AppProfilesConnector />

        <ManageApplicationsModal
          isOpen={isManageApplicationsOpen}
          onModalClose={onManageApplicationsModalClose}
        />
      </PageContentBody>
    </PageContent>
  );
}

export default ApplicationSettings;
