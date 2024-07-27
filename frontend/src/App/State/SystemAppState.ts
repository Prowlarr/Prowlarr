import Health from 'typings/Health';
import SystemStatus from 'typings/SystemStatus';
import Task from 'typings/Task';
import Update from 'typings/Update';
import AppSectionState, { AppSectionItemState } from './AppSectionState';

export type HealthAppState = AppSectionState<Health>;
export type SystemStatusAppState = AppSectionItemState<SystemStatus>;
export type TaskAppState = AppSectionState<Task>;
export type UpdateAppState = AppSectionState<Update>;

interface SystemAppState {
  health: HealthAppState;
  status: SystemStatusAppState;
  tasks: TaskAppState;
  updates: UpdateAppState;
}

export default SystemAppState;
