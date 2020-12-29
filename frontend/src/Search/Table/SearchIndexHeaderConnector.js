import { connect } from 'react-redux';
import { setReleasesTableOption } from 'Store/Actions/releaseActions';
import SearchIndexHeader from './SearchIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setReleasesTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(SearchIndexHeader);
