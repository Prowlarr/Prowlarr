import PropTypes from 'prop-types';
import React from 'react';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';
import SearchIndexFilterModalConnector from 'Search/SearchIndexFilterModalConnector';

function SearchIndexFilterMenu(props) {
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

SearchIndexFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number])
    .isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
};

SearchIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default SearchIndexFilterMenu;
