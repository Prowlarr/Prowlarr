import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import MovieSearchInput from './MovieSearchInput';

function createCleanMovieSelector() {
  return createSelector(
    createAllIndexersSelector(),
    createTagsSelector(),
    (allMovies, allTags) => {
      return allMovies.map((movie) => {
        const {
          name,
          titleSlug,
          sortTitle,
          year,
          images,
          alternateTitles = [],
          tags = []
        } = movie;

        return {
          name,
          titleSlug,
          sortTitle,
          year,
          images,
          alternateTitles,
          firstCharacter: name.charAt(0).toLowerCase(),
          tags: tags.reduce((acc, id) => {
            const matchingTag = allTags.find((tag) => tag.id === id);

            if (matchingTag) {
              acc.push(matchingTag);
            }

            return acc;
          }, [])
        };
      });
    }
  );
}

function createMapStateToProps() {
  return createDeepEqualSelector(
    createCleanMovieSelector(),
    (movies) => {
      return {
        movies
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onGoToMovie(titleSlug) {
      dispatch(push(`${window.Prowlarr.urlBase}/movie/${titleSlug}`));
    },

    onGoToAddNewMovie(query) {
      dispatch(push(`${window.Prowlarr.urlBase}/add/new?term=${encodeURIComponent(query)}`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(MovieSearchInput);
