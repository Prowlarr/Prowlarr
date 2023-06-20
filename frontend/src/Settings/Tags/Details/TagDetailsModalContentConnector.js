import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import TagDetailsModalContent from './TagDetailsModalContent';

function findMatchingItems(ids, items) {
  return items.filter((s) => {
    return ids.includes(s.id);
  });
}

function createMatchingIndexersSelector() {
  return createSelector(
    (state, { indexerIds }) => indexerIds,
    (state) => state.indexers.items,
    findMatchingItems
  );
}

function createMatchingIndexerProxiesSelector() {
  return createSelector(
    (state, { indexerProxyIds }) => indexerProxyIds,
    (state) => state.settings.indexerProxies.items,
    findMatchingItems
  );
}

function createMatchingNotificationsSelector() {
  return createSelector(
    (state, { notificationIds }) => notificationIds,
    (state) => state.settings.notifications.items,
    findMatchingItems
  );
}

function createMatchingApplicationsSelector() {
  return createSelector(
    (state, { applicationIds }) => applicationIds,
    (state) => state.settings.applications.items,
    findMatchingItems
  );
}

function createMapStateToProps() {
  return createSelector(
    createMatchingIndexersSelector(),
    createMatchingIndexerProxiesSelector(),
    createMatchingNotificationsSelector(),
    createMatchingApplicationsSelector(),
    (indexers, indexerProxies, notifications, applications) => {
      return {
        indexers,
        indexerProxies,
        notifications,
        applications
      };
    }
  );
}

export default connect(createMapStateToProps)(TagDetailsModalContent);
