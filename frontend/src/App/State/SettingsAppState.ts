import AppSectionState, {
  AppSectionDeleteState,
  AppSectionItemState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import { IndexerCategory } from 'Indexer/Indexer';
import Application from 'typings/Application';
import DownloadClient from 'typings/DownloadClient';
import Notification from 'typings/Notification';
import General from 'typings/Settings/General';
import UiSettings from 'typings/Settings/UiSettings';

export interface AppProfileAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface ApplicationAppState
  extends AppSectionState<Application>,
    AppSectionDeleteState,
    AppSectionSaveState {
  isTestingAll: boolean;
}

export interface DownloadClientAppState
  extends AppSectionState<DownloadClient>,
    AppSectionDeleteState,
    AppSectionSaveState {
  isTestingAll: boolean;
}

export interface GeneralAppState
  extends AppSectionItemState<General>,
    AppSectionSaveState {}

export interface IndexerCategoryAppState
  extends AppSectionState<IndexerCategory>,
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
  general: GeneralAppState;
  indexerCategories: IndexerCategoryAppState;
  notifications: NotificationAppState;
  ui: UiSettingsAppState;
}

export default SettingsAppState;
