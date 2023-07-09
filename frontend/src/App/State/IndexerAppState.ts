import Indexer from 'typings/Indexer';
import AppSectionState, {
  AppSectionDeleteState,
  AppSectionSaveState,
} from './AppSectionState';

interface IndexerAppState
  extends AppSectionState<Indexer>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export default IndexerAppState;
