import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setIndexerFilter } from 'Store/Actions/indexerIndexActions';

function createIndexerSelector() {
  return createSelector(
    (state) => state.indexers.items,
    (indexers) => {
      return indexers;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state) => state.indexerIndex.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

export default function IndexerIndexFilterModal(props) {
  const sectionItems = useSelector(createIndexerSelector());
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());
  const customFilterType = 'indexerIndex';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload) => {
      dispatch(setIndexerFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      {...props}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType={customFilterType}
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
