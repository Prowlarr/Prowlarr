import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { bulkDeleteIndexers } from 'Store/Actions/indexerIndexActions';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import DeleteIndexerModalContent from './DeleteIndexerModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    createAllIndexersSelector(),
    (indexerIds, allIndexers) => {
      const selectedMovie = _.intersectionWith(allIndexers, indexerIds, (s, id) => {
        return s.id === id;
      });

      const sortedMovies = _.orderBy(selectedMovie, 'name');
      const indexers = _.map(sortedMovies, (s) => {
        return {
          name: s.name
        };
      });

      return {
        indexers
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onDeleteSelectedPress() {
      dispatch(bulkDeleteIndexers({
        indexerIds: props.indexerIds
      }));

      props.onModalClose();
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(DeleteIndexerModalContent);
