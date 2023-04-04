import React from 'react';
import { CustomFilter } from 'App/State/AppState';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';
import IndexerIndexFilterModal from 'Indexer/Index/IndexerIndexFilterModal';

interface IndexerIndexFilterMenuProps {
  selectedFilterKey: string | number;
  filters: object[];
  customFilters: CustomFilter[];
  isDisabled: boolean;
  onFilterSelect(filterName: string): unknown;
}

function IndexerIndexFilterMenu(props: IndexerIndexFilterMenuProps) {
  const {
    selectedFilterKey,
    filters,
    customFilters,
    isDisabled,
    onFilterSelect,
  } = props;

  return (
    <FilterMenu
      alignMenu={align.RIGHT}
      isDisabled={isDisabled}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      customFilters={customFilters}
      filterModalConnectorComponent={IndexerIndexFilterModal}
      onFilterSelect={onFilterSelect}
    />
  );
}

IndexerIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default IndexerIndexFilterMenu;
