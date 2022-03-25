import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllIndexersSelector from './createAllIndexersSelector';

function createProfileInUseSelector(profileProp) {
  return createSelector(
    (state, { id }) => id,
    (state) => state.settings.appProfiles.items,
    createAllIndexersSelector(),
    (id, profiles, indexers) => {
      if (!id) {
        return false;
      }

      if (_.some(indexers, { [profileProp]: id }) || profiles.length <= 1) {
        return true;
      }

      return false;
    }
  );
}

export default createProfileInUseSelector;
