import { some } from 'lodash-es';
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

      return some(indexers, { [profileProp]: id }) || profiles.length <= 1;
    }
  );
}

export default createProfileInUseSelector;
