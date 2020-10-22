import _ from 'lodash';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import TagsModalContent from './TagsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    createAllIndexersSelector(),
    createTagsSelector(),
    (indexerIds, allMovies, tagList) => {
      const movies = _.intersectionWith(allMovies, indexerIds, (s, id) => {
        return s.id === id;
      });

      const movieTags = _.uniq(_.concat(..._.map(movies, 'tags')));

      return {
        movieTags,
        tagList
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onAction() {
      // Do something
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(TagsModalContent);
