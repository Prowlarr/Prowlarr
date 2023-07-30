import { some } from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import createAllIndexersSelector from './createAllIndexersSelector';

function createProfileInUseSelector(profileProp: string) {
  return createSelector(
    (_: AppState, { id }: { id: number }) => id,
    (state: AppState) => state.settings.appProfiles.items,
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
