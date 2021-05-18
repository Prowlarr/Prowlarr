import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllIndexersSelector from './createAllIndexersSelector';

function createProfileInUseSelector(profileProp) {
  return createSelector(
    (state, { id }) => id,
    createAllIndexersSelector(),
    (id, indexers) => {
      if (!id) {
        return false;
      }

      if (_.some(indexers, { [profileProp]: id })) {
        return true;
      }

      return false;
    }
  );
}

export default createProfileInUseSelector;
