import AppSectionState, {
  AppSectionFilterState,
} from 'App/State/AppSectionState';
import Column from 'Components/Table/Column';
import History from 'typings/History';

interface HistoryAppState
  extends AppSectionState<History>,
    AppSectionFilterState<History> {
  pageSize: number;
  columns: Column[];
}

export default HistoryAppState;
