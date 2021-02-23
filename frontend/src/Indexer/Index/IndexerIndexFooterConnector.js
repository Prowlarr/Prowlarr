import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import IndexerIndexFooter from './IndexerIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('indexers', 'indexerIndex'),
    (indexers) => {
      return indexers.items.map((s) => {
        const {
          protocol,
          privacy,
          enable
        } = s;

        return {
          protocol,
          privacy,
          enable
        };
      });
    }
  );
}

function createMoviesSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (movies) => movies
  );
}

function createMapStateToProps() {
  return createSelector(
    createMoviesSelector(),
    createUISettingsSelector(),
    (movies, uiSettings) => {
      return {
        movies,
        colorImpairedMode: uiSettings.enableColorImpairedMode
      };
    }
  );
}

export default connect(createMapStateToProps)(IndexerIndexFooter);
