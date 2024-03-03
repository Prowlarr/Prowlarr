import CommandAppState from './CommandAppState';
import HistoryAppState from './HistoryAppState';
import IndexerAppState, {
  IndexerHistoryAppState,
  IndexerIndexAppState,
  IndexerStatusAppState,
} from './IndexerAppState';
import IndexerStatsAppState from './IndexerStatsAppState';
import SettingsAppState from './SettingsAppState';
import SystemAppState from './SystemAppState';
import TagsAppState from './TagsAppState';

interface FilterBuilderPropOption {
  id: string;
  name: string;
}

export interface FilterBuilderProp<T> {
  name: string;
  label: string;
  type: string;
  valueType?: string;
  optionsSelector?: (items: T[]) => FilterBuilderPropOption[];
}

export interface PropertyFilter {
  key: string;
  value: boolean | string | number | string[] | number[];
  type: string;
}

export interface Filter {
  key: string;
  label: string;
  filers: PropertyFilter[];
}

export interface CustomFilter {
  id: number;
  type: string;
  label: string;
  filers: PropertyFilter[];
}

export interface AppSectionState {
  isConnected: boolean;
  isReconnecting: boolean;
  version: string;
  prevVersion?: string;
  dimensions: {
    isSmallScreen: boolean;
    width: number;
    height: number;
  };
}

interface AppState {
  app: AppSectionState;
  commands: CommandAppState;
  history: HistoryAppState;
  indexerHistory: IndexerHistoryAppState;
  indexerIndex: IndexerIndexAppState;
  indexerStats: IndexerStatsAppState;
  indexerStatus: IndexerStatusAppState;
  indexers: IndexerAppState;
  settings: SettingsAppState;
  system: SystemAppState;
  tags: TagsAppState;
}

export default AppState;
