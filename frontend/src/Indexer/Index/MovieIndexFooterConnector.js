import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import MovieIndexFooter from './MovieIndexFooter';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('indexers', 'indexerIndex'),
    (indexers) => {
      return indexers.items.map((s) => {
        const {
          protocol,
          privacy
        } = s;

        return {
          protocol,
          privacy
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

export default connect(createMapStateToProps)(MovieIndexFooter);
