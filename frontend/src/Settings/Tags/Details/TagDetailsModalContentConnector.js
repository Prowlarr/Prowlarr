import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import TagDetailsModalContent from './TagDetailsModalContent';

function findMatchingItems(ids, items) {
  return items.filter((s) => {
    return ids.includes(s.id);
  });
}

function createUnorderedMatchingMoviesSelector() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    createAllIndexersSelector(),
    findMatchingItems
  );
}

function createMatchingMoviesSelector() {
  return createSelector(
    createUnorderedMatchingMoviesSelector(),
    (movies) => {
      return movies.sort((movieA, movieB) => {
        const sortTitleA = movieA.sortTitle;
        const sortTitleB = movieB.sortTitle;

        if (sortTitleA > sortTitleB) {
          return 1;
        } else if (sortTitleA < sortTitleB) {
          return -1;
        }

        return 0;
      });
    }
  );
}

function createMatchingNotificationsSelector() {
  return createSelector(
    (state, { notificationIds }) => notificationIds,
    (state) => state.settings.notifications.items,
    findMatchingItems
  );
}

function createMapStateToProps() {
  return createSelector(
    createMatchingMoviesSelector(),
    createMatchingNotificationsSelector(),
    (movies, notifications) => {
      return {
        movies,
        notifications
      };
    }
  );
}

export default connect(createMapStateToProps)(TagDetailsModalContent);
