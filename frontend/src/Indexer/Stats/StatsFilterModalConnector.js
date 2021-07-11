import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setIndexerStatsFilter } from 'Store/Actions/indexerStatsActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.indexerStats.items,
    (state) => state.indexerStats.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'indexerStats'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setIndexerStatsFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
