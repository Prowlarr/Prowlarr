import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterBuilderRowValue from './FilterBuilderRowValue';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.appProfiles,
    (appProfiles) => {
      const tagList = appProfiles.items.map((appProfile) => {
        const {
          id,
          name
        } = appProfile;

        return {
          id,
          name
        };
      });

      return {
        tagList
      };
    }
  );
}

export default connect(createMapStateToProps)(FilterBuilderRowValue);
