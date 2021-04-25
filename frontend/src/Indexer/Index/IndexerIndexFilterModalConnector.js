import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setMovieFilter } from 'Store/Actions/indexerIndexActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexers.items,
    (state) => state.indexerIndex.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'indexerIndex'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setMovieFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
