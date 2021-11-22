import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import { sortDirections } from 'Helpers/Props';
import IndexerIndexItemConnector from 'Indexer/Index/IndexerIndexItemConnector';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import IndexerIndexHeaderConnector from './IndexerIndexHeaderConnector';
import IndexerIndexRow from './IndexerIndexRow';
import styles from './IndexerIndexTable.css';

class IndexerIndexTable extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scrollIndex: null
    };
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      jumpToCharacter
    } = this.props;

    if (jumpToCharacter != null && jumpToCharacter !== prevProps.jumpToCharacter) {

      const scrollIndex = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (scrollIndex != null) {
        this.setState({ scrollIndex });
      }
    } else if (jumpToCharacter == null && prevProps.jumpToCharacter != null) {
      this.setState({ scrollIndex: null });
    }
  }

  //
  // Control

  rowRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      columns,
      selectedState,
      onSelectedChange,
      isMovieEditorActive
    } = this.props;

    const movie = items[rowIndex];

    return (
      <VirtualTableRow
        key={key}
        style={style}
      >
        <IndexerIndexItemConnector
          key={movie.id}
          component={IndexerIndexRow}
          columns={columns}
          indexerId={movie.id}
          isSelected={selectedState[movie.id]}
          onSelectedChange={onSelectedChange}
          isMovieEditorActive={isMovieEditorActive}
        />
      </VirtualTableRow>
    );
  };

  //
  // Render

  render() {
    const {
      items,
      columns,
      sortKey,
      sortDirection,
      isSmallScreen,
      onSortPress,
      scroller,
      allSelected,
      allUnselected,
      onSelectAllChange,
      isMovieEditorActive,
      selectedState
    } = this.props;

    return (
      <VirtualTable
        className={styles.tableContainer}
        items={items}
        scrollIndex={this.state.scrollIndex}
        isSmallScreen={isSmallScreen}
        scroller={scroller}
        rowHeight={38}
        overscanRowCount={2}
        rowRenderer={this.rowRenderer}
        header={
          <IndexerIndexHeaderConnector
            columns={columns}
            sortKey={sortKey}
            sortDirection={sortDirection}
            onSortPress={onSortPress}
            allSelected={allSelected}
            allUnselected={allUnselected}
            onSelectAllChange={onSelectAllChange}
            isMovieEditorActive={isMovieEditorActive}
          />
        }
        selectedState={selectedState}
        columns={columns}
      />
    );
  }
}

IndexerIndexTable.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  jumpToCharacter: PropTypes.string,
  isSmallScreen: PropTypes.bool.isRequired,
  scroller: PropTypes.instanceOf(Element).isRequired,
  onSortPress: PropTypes.func.isRequired,
  allSelected: PropTypes.bool.isRequired,
  allUnselected: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onSelectAllChange: PropTypes.func.isRequired,
  isMovieEditorActive: PropTypes.bool.isRequired
};

export default IndexerIndexTable;
