import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Grid, WindowScroller } from 'react-virtualized';
import Measure from 'Components/Measure';
import SearchIndexItemConnector from 'Search/Table/SearchIndexItemConnector';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import SearchIndexOverview from './SearchIndexOverview';
import styles from './SearchIndexOverviews.css';

class SearchIndexOverviews extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      width: 0,
      columnCount: 1,
      rowHeight: 100,
      scrollRestored: false
    };

    this._grid = null;
  }

  componentDidUpdate(prevProps, prevState) {
    const {
      items,
      sortKey,
      jumpToCharacter,
      scrollTop,
      isSmallScreen
    } = this.props;

    const {
      width,
      rowHeight,
      scrollRestored
    } = this.state;

    if (prevProps.sortKey !== sortKey) {
      this.calculateGrid(this.state.width, isSmallScreen);
    }

    if (
      this._grid &&
        (prevState.width !== width ||
            prevState.rowHeight !== rowHeight ||
            hasDifferentItemsOrOrder(prevProps.items, items, 'guid')
        )
    ) {
      // recomputeGridSize also forces Grid to discard its cache of rendered cells
      this._grid.recomputeGridSize();
    }

    if (this._grid && scrollTop !== 0 && !scrollRestored) {
      this.setState({ scrollRestored: true });
      this._grid.scrollToPosition({ scrollTop });
    }

    if (jumpToCharacter != null && jumpToCharacter !== prevProps.jumpToCharacter) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (this._grid && index != null) {

        this._grid.scrollToCell({
          rowIndex: index,
          columnIndex: 0
        });
      }
    }
  }

  //
  // Control

  setGridRef = (ref) => {
    this._grid = ref;
  };

  calculateGrid = (width = this.state.width, isSmallScreen) => {
    const rowHeight = 100;

    this.setState({
      width,
      rowHeight
    });
  };

  cellRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      isSmallScreen,
      onGrabPress
    } = this.props;

    const {
      rowHeight
    } = this.state;

    const release = items[rowIndex];

    return (
      <div
        className={styles.container}
        key={key}
        style={style}
      >
        <SearchIndexItemConnector
          key={release.guid}
          component={SearchIndexOverview}
          rowHeight={rowHeight}
          showRelativeDates={showRelativeDates}
          shortDateFormat={shortDateFormat}
          longDateFormat={longDateFormat}
          timeFormat={timeFormat}
          isSmallScreen={isSmallScreen}
          style={style}
          guid={release.guid}
          onGrabPress={onGrabPress}
        />
      </div>
    );
  };

  //
  // Listeners

  onMeasure = ({ width }) => {
    this.calculateGrid(width, this.props.isSmallScreen);
  };

  //
  // Render

  render() {
    const {
      items
    } = this.props;

    const {
      width,
      rowHeight
    } = this.state;

    return (
      <Measure
        whitelist={['width']}
        onMeasure={this.onMeasure}
      >
        <WindowScroller
          scrollElement={undefined}
        >
          {({ height, registerChild, onChildScroll, scrollTop }) => {
            if (!height) {
              return <div />;
            }

            return (
              <div ref={registerChild}>
                <Grid
                  ref={this.setGridRef}
                  className={styles.grid}
                  autoHeight={true}
                  height={height}
                  columnCount={1}
                  columnWidth={width}
                  rowCount={items.length}
                  rowHeight={rowHeight}
                  width={width}
                  onScroll={onChildScroll}
                  scrollTop={scrollTop}
                  overscanRowCount={2}
                  cellRenderer={this.cellRenderer}
                  scrollToAlignment={'start'}
                  isScrollingOptOut={true}
                />
              </div>
            );
          }
          }
        </WindowScroller>
      </Measure>
    );
  }
}

SearchIndexOverviews.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  scrollTop: PropTypes.number,
  jumpToCharacter: PropTypes.string,
  scroller: PropTypes.instanceOf(Element).isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  timeFormat: PropTypes.string.isRequired,
  onGrabPress: PropTypes.func.isRequired
};

export default SearchIndexOverviews;
