import { AppSectionItemState } from 'App/State/AppSectionState';
import { Filter, FilterBuilderProp } from 'App/State/AppState';
import Indexer from 'Indexer/Indexer';
import { IndexerStats } from 'typings/IndexerStats';

export interface IndexerStatsAppState
  extends AppSectionItemState<IndexerStats> {
  filterBuilderProps: FilterBuilderProp<Indexer>[];
  selectedFilterKey: string;
  filters: Filter[];
}

export default IndexerStatsAppState;
