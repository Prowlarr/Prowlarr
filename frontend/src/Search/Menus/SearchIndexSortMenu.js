import PropTypes from 'prop-types';
import React from 'react';
import MenuContent from 'Components/Menu/MenuContent';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import { align, sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

function SearchIndexSortMenu(props) {
  const {
    sortKey,
    sortDirection,
    isDisabled,
    onSortSelect
  } = props;

  return (
    <SortMenu
      isDisabled={isDisabled}
      alignMenu={align.RIGHT}
    >
      <MenuContent>
        <SortMenuItem
          name="protocol"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Protocol')}
        </SortMenuItem>

        <SortMenuItem
          name="age"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Age')}
        </SortMenuItem>

        <SortMenuItem
          name="title"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Title')}
        </SortMenuItem>

        <SortMenuItem
          name="indexer"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Indexer')}
        </SortMenuItem>

        <SortMenuItem
          name="size"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Size')}
        </SortMenuItem>

        <SortMenuItem
          name="files"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Files')}
        </SortMenuItem>

        <SortMenuItem
          name="grabs"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Grabs')}
        </SortMenuItem>

        <SortMenuItem
          name="peers"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Peers')}
        </SortMenuItem>

        <SortMenuItem
          name="category"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Category')}
        </SortMenuItem>
      </MenuContent>
    </SortMenu>
  );
}

SearchIndexSortMenu.propTypes = {
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  isDisabled: PropTypes.bool.isRequired,
  onSortSelect: PropTypes.func.isRequired
};

export default SearchIndexSortMenu;
