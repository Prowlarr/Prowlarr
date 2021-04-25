import PropTypes from 'prop-types';
import React from 'react';
import MenuContent from 'Components/Menu/MenuContent';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import { align, sortDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

function IndexerIndexSortMenu(props) {
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
          name="status"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          Status
        </SortMenuItem>

        <SortMenuItem
          name="name"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Name')}
        </SortMenuItem>

        <SortMenuItem
          name="added"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {translate('Added')}
        </SortMenuItem>

        <SortMenuItem
          name="priority"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {'Priority'}
        </SortMenuItem>

        <SortMenuItem
          name="protocol"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {'Protocol'}
        </SortMenuItem>

        <SortMenuItem
          name="privacy"
          sortKey={sortKey}
          sortDirection={sortDirection}
          onPress={onSortSelect}
        >
          {'Privacy'}
        </SortMenuItem>
      </MenuContent>
    </SortMenu>
  );
}

IndexerIndexSortMenu.propTypes = {
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  isDisabled: PropTypes.bool.isRequired,
  onSortSelect: PropTypes.func.isRequired
};

export default IndexerIndexSortMenu;
