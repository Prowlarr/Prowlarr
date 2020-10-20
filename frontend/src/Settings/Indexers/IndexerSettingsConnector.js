import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { testAllIndexers } from 'Store/Actions/indexerActions';
import IndexerSettings from './IndexerSettings';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexers.isTestingAll,
    (isTestingAll) => {
      return {
        isTestingAll
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchTestAllIndexers: testAllIndexers
};

export default connect(createMapStateToProps, mapDispatchToProps)(IndexerSettings);
