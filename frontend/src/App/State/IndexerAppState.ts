import Column from 'Components/Table/Column';
import SortDirection from 'Helpers/Props/SortDirection';
import Indexer, { IndexerStatus } from 'Indexer/Indexer';
import History from 'typings/History';
import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
} from './AppSectionState';
import { Filter, FilterBuilderProp } from './AppState';

export interface IndexerIndexAppState {
  isTestingAll: boolean;
  sortKey: string;
  sortDirection: SortDirection;
  secondarySortKey: string;
  secondarySortDirection: SortDirection;
  view: string;

  tableOptions: {
    showSearchAction: boolean;
  };

  selectedFilterKey: string;
  filterBuilderProps: FilterBuilderProp<Indexer>[];
  filters: Filter[];
  columns: Column[];
}

interface IndexerAppState
  extends AppSectionState<Indexer>,
    AppSectionDeleteState,
    AppSectionSaveState {
  itemMap: Record<number, number>;

  isTestingAll: boolean;
}

export type IndexerStatusAppState = AppSectionState<IndexerStatus>;

export type IndexerHistoryAppState = AppSectionState<History>;

export default IndexerAppState;
