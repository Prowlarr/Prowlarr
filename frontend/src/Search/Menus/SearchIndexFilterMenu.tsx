import React from 'react';
import { CustomFilter } from 'App/State/AppState';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';
import SearchIndexFilterModalConnector from 'Search/SearchIndexFilterModalConnector';

interface SearchIndexFilterMenuProps {
  selectedFilterKey: string | number;
  filters: object[];
  customFilters: CustomFilter[];
  isDisabled: boolean;
  onFilterSelect(filterName: string): unknown;
}

function SearchIndexFilterMenu(props: SearchIndexFilterMenuProps) {
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
      filterModalConnectorComponent={SearchIndexFilterModalConnector}
      onFilterSelect={onFilterSelect}
    />
  );
}

SearchIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default SearchIndexFilterMenu;
