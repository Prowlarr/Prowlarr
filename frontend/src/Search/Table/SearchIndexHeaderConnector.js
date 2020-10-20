import { connect } from 'react-redux';
import { setMovieTableOption } from 'Store/Actions/indexerIndexActions';
import SearchIndexHeader from './SearchIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setMovieTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(SearchIndexHeader);
