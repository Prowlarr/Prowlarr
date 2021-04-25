import { push } from 'connected-react-router';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSearchDefault } from 'Store/Actions/releaseActions';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import IndexerSearchInput from './IndexerSearchInput';

function createCleanMovieSelector() {
  return createSelector(
    createAllIndexersSelector(),
    createTagsSelector(),
    (allIndexers, allTags) => {
      return allIndexers.map((movie) => {
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
    onGoToAddNewMovie(query) {
      dispatch(setSearchDefault({ searchQuery: query, searchIndexerIds: [-1, -2] }));
      dispatch(push(`${window.Prowlarr.urlBase}/search`));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(IndexerSearchInput);
