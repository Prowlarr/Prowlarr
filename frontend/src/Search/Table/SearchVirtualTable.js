import PropTypes from 'prop-types';
import React, { useEffect, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import { GRAB_RELEASE, SET_RELEASES_SORT } from 'Store/Actions/releaseActions';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import SearchVirtualRow from './SearchVirtualRow';
import VirtualTable from './VirtualTable';
import styles from './SearchIndexTable.css';

function createMapStateToProps() {
  return createSelector(
    (state) => state.app.dimensions,
    createUISettingsSelector(),
    (dimensions, uiSettings) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        longDateFormat: uiSettings.longDateFormat,
        timeFormat: uiSettings.timeFormat
      };
    }
  );
}

// Allows the fetching of previous values
// As it is needed in componentDidUpdate
function usePrevious(value) {
  const ref = useRef();
  useEffect(() => {
    ref.current = value;
  });
  return ref.current;
}

function SearchVirtualTable({ items, jumpToCharacter, columns, scroller, sortKey, sortDirection }) {
  const [scrollIndex, setScrollIndex] = useState(null);
  const { isSmallScreen, longDateFormat, timeFormat } = useSelector(createMapStateToProps());
  const prevJumpToCharacter = usePrevious(jumpToCharacter);
  const dispatch = useDispatch();

  // Replacement for the componentDidUpdate
  useEffect(() => {
    if (jumpToCharacter !== null && jumpToCharacter !== prevJumpToCharacter) {
      const innerScrollIndex = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (innerScrollIndex !== null) {
        setScrollIndex({ innerScrollIndex });
      }
    } else if (jumpToCharacter === null && prevJumpToCharacter !== null) {
      setScrollIndex(null);
    }
  }, [jumpToCharacter]);

  function onSortPress(eventSortKey) {
    dispatch({ type: SET_RELEASES_SORT, payload: { sortKey: eventSortKey } });
  }

  function onGrabPress(payload) {
    dispatch({ type: GRAB_RELEASE, payload });
  }

  function rowRenderer({ index, style }) {
    const release = items[index];

    return (
      <VirtualTableRow
        style={style}
      >
        <SearchVirtualRow
          columns={columns}
          guid={release.guid}
          longDateFormat={longDateFormat}
          timeFormat={timeFormat}
          onGrabPress={onGrabPress}
          rowData={release}
        />
      </VirtualTableRow>
    );
  }

  return (
    <VirtualTable
      className={styles.tableContainer}
      items={items}
      scrollIndex={scrollIndex}
      isSmallScreen={isSmallScreen}
      scroller={scroller}
      rowHeight={38}
      overscanRowCount={2}
      rowRenderer={rowRenderer}
      columns={columns}
      onSortPress={onSortPress}
      sortKey={sortKey}
      sortDirection={sortDirection}
    />
  );
}

SearchVirtualTable.propTypes = {
  items: PropTypes.array.isRequired,
  jumpToCharacter: PropTypes.func.isRequired,
  columns: PropTypes.array.isRequired,
  scroller: PropTypes.func.isRequired,
  sortKey: PropTypes.string.isRequired,
  sortDirection: PropTypes.string.isRequired
};

export default SearchVirtualTable;
