import AppSectionState from 'App/State/AppSectionState';
import History from 'typings/History';

interface HistoryAppState extends AppSectionState<History> {
  pageSize: number;
}

export default HistoryAppState;
