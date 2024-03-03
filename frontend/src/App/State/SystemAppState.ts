import SystemStatus from 'typings/SystemStatus';
import Update from 'typings/Update';
import AppSectionState, { AppSectionItemState } from './AppSectionState';

export type SystemStatusAppState = AppSectionItemState<SystemStatus>;
export type UpdateAppState = AppSectionState<Update>;

interface SystemAppState {
  status: SystemStatusAppState;
  updates: UpdateAppState;
}

export default SystemAppState;
