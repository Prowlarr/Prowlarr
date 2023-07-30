import AppSectionState, {
  AppSectionDeleteState,
} from 'App/State/AppSectionState';
import Release from 'typings/Release';

interface ReleaseAppState
  extends AppSectionState<Release>,
    AppSectionDeleteState {}

export default ReleaseAppState;
