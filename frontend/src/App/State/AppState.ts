import IndexerAppState, { IndexerIndexAppState } from './IndexerAppState';
import SettingsAppState from './SettingsAppState';
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

interface AppState {
  indexerIndex: IndexerIndexAppState;
  indexers: IndexerAppState;
  settings: SettingsAppState;
  tags: TagsAppState;
}

export default AppState;
