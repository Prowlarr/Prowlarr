import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FilterModal from 'Components/Filter/FilterModal';
import { setIndexerStatsFilter } from 'Store/Actions/indexerStatsActions';

function createIndexerStatsSelector() {
  return createSelector(
    (state: AppState) => state.indexerStats.item,
    (indexerStats) => {
      return indexerStats;
    }
  );
}

function createFilterBuilderPropsSelector() {
  return createSelector(
    (state: AppState) => state.indexerStats.filterBuilderProps,
    (filterBuilderProps) => {
      return filterBuilderProps;
    }
  );
}

interface IndexerStatsFilterModalProps {
  isOpen: boolean;
}

export default function IndexerStatsFilterModal(
  props: IndexerStatsFilterModalProps
) {
  const sectionItems = [useSelector(createIndexerStatsSelector())];
  const filterBuilderProps = useSelector(createFilterBuilderPropsSelector());
  const customFilterType = 'indexerStats';

  const dispatch = useDispatch();

  const dispatchSetFilter = useCallback(
    (payload: unknown) => {
      dispatch(setIndexerStatsFilter(payload));
    },
    [dispatch]
  );

  return (
    <FilterModal
      // TODO: Don't spread all the props
      {...props}
      sectionItems={sectionItems}
      filterBuilderProps={filterBuilderProps}
      customFilterType={customFilterType}
      dispatchSetFilter={dispatchSetFilter}
    />
  );
}
