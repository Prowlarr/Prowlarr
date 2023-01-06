import PropTypes from 'prop-types';
import React from 'react';
import FilterMenu from 'Components/Menu/FilterMenu';
import { align } from 'Helpers/Props';
import IndexerIndexFilterModal from 'Indexer/Index/IndexerIndexFilterModal';

function IndexerIndexFilterMenu(props) {
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

IndexerIndexFilterMenu.propTypes = {
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number])
    .isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  isDisabled: PropTypes.bool.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
};

IndexerIndexFilterMenu.defaultProps = {
  showCustomFilters: false,
};

export default IndexerIndexFilterMenu;
