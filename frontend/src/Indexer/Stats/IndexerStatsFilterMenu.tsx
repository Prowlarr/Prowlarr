import React from 'react';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';

interface IndexerStatsFilterMenuProps {
  selectedFilterKey: string | number;
  filters: object[];
  isDisabled: boolean;
  onFilterSelect(filterName: string): unknown;
}

function IndexerStatsFilterMenu(props: IndexerStatsFilterMenuProps) {
  const { selectedFilterKey, filters, isDisabled, onFilterSelect } = props;

  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={[]}
      onFilterSelect={onFilterSelect}
    />
  );
}

export default IndexerStatsFilterMenu;
