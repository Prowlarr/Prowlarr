import { AppSectionItemState } from 'App/State/AppSectionState';
import { Filter } from 'App/State/AppState';
import { IndexerStats } from 'typings/IndexerStats';

export interface IndexerStatsAppState
  extends AppSectionItemState<IndexerStats> {
  selectedFilterKey: string;
  filters: Filter[];
}

export default IndexerStatsAppState;
