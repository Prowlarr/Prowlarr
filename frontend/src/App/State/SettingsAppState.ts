import AppSectionState, {
  AppSectionDeleteState,
  AppSectionItemState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import Application from 'typings/Application';
import DownloadClient from 'typings/DownloadClient';
import Notification from 'typings/Notification';
import { UiSettings } from 'typings/UiSettings';

export interface AppProfileAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface ApplicationAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface DownloadClientAppState
  extends AppSectionState<DownloadClient>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface NotificationAppState
  extends AppSectionState<Notification>,
    AppSectionDeleteState {}

export type UiSettingsAppState = AppSectionItemState<UiSettings>;

interface SettingsAppState {
  appProfiles: AppProfileAppState;
  applications: ApplicationAppState;
  downloadClients: DownloadClientAppState;
  notifications: NotificationAppState;
  ui: UiSettingsAppState;
}

export default SettingsAppState;
