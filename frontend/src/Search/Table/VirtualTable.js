import PropTypes from 'prop-types';
import React, { forwardRef, useCallback, useEffect, useState } from 'react';
import { VariableSizeList } from 'react-window';
import { ReactWindowScroller } from 'react-window-scroller';
import Measure from 'Components/Measure';
import Scroller from 'Components/Scroller/Scroller';
import { scrollDirections } from '../../Helpers/Props';
import SearchIndexHeaderConnector from './SearchIndexHeaderConnector';

// Wrapper for Scroller as it needs callback
// Used https://github.com/bvaughn/react-window/issues/110#issuecomment-469061213
// As a baseplate for how to implement
function ScrollerWrapperWrapper({ onScroll, forwardedRef, style, children }) {
  const refSetter = useCallback((scrollbarsRef) => {
    if (scrollbarsRef) {
      forwardedRef(scrollbarsRef.view);
    } else {
      forwardedRef(null);
    }
  }, []);

  return (
    <Scroller
      ref={refSetter}
      style={style}
      onScroll={onScroll}
      scrollDirection={scrollDirections.HORIZONTAL}
    >
      {children}
    </Scroller>
  );
}

ScrollerWrapperWrapper.propTypes = {
  onScroll: PropTypes.func.isRequired,
  forwardedRef: PropTypes.func.isRequired,
  style: PropTypes.object.isRequired,
  children: PropTypes.node.isRequired
};

const ScrollerWrapper = forwardRef((props, ref) => (
  <ScrollerWrapperWrapper {...props} forwardedRef={ref} />
));

// Calculates the width of a given amount of text using the default font family
// Might be worth looking into if we can check exactly which is used, but that is unlikely
// Based from https://stackoverflow.com/a/21015393/6216166
function getItemWidth({ sortTitle }) {
  function getTextWidth(text, font) {
    const canvas = getTextWidth.canvas || (getTextWidth.canvas = document.createElement('canvas'));
    const searchHeaderTitle = getTextWidth.searchHeaderTitle || (getTextWidth.searchHeaderTitle = document.getElementById('searchHeaderTitle'));
    const computedSearchHeaderTitle = window.getComputedStyle(searchHeaderTitle);
    const context = canvas.getContext('2d');
    context.font = font;
    const metrics = context.measureText(text);
    // Calculates the amount of column widths needed to fit the text
    return Math.ceil(metrics.width / (searchHeaderTitle.offsetWidth - (parseInt(computedSearchHeaderTitle.paddingLeft) * 2)));
  }

  // Multiples up by the default line height
  return getTextWidth(sortTitle, '14px Roboto') * 38;
}

function VirtualTable({
  className,
  focusScroller,
  scroller,
  items,
  rowRenderer,
  isSmallScreen,
  columns,
  sortKey,
  sortDirection,
  onSortPress
}) {
  const [width, setWidth] = useState(0);
  const [height, setHeight] = useState(0);
  const [titleReady, setTitleReady] = useState(false);
  const [rowHeights, setRowHeights] = useState([]);
  // Height of its parent, taking away the header & magic number so it doesn't scroll
  const computedOffsetHeight = scroller.offsetHeight - 37 - 60;

  useEffect(() => {
    if (titleReady) {
      // Returns an array of ints for the height of each title
      setRowHeights(items.map(getItemWidth));
    }
  }, [titleReady]);

  useEffect(() => {
    setHeight(isSmallScreen ? window.innerHeight : computedOffsetHeight);
  }, []);

  useEffect(() => {
    setHeight(isSmallScreen ? window.innerHeight : computedOffsetHeight);
  }, [isSmallScreen]);

  function onMeasure({ width: onMeasureWidth }) {
    setWidth(onMeasureWidth);
  }

  function getItemSize(index) {
    return rowHeights[index];
  }

  return (
    <ReactWindowScroller>
      {({ ref, outerRef, style, onScroll }) => {
        return (
          <>
            <Measure
              whitelist={['width']}
              onMeasure={onMeasure}
            >
              <Scroller
                className={className}
                scrollDirection={scrollDirections.NONE}
                autoFocus={focusScroller}
              >
                <SearchIndexHeaderConnector
                  columns={columns}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  onSortPress={onSortPress}
                  setTitleReady={setTitleReady}
                />
                {rowHeights.length !== 0 &&
                  <VariableSizeList
                    ref={isSmallScreen ? ref : undefined}
                    outerRef={isSmallScreen ? outerRef : undefined}
                    style={isSmallScreen ? style : undefined}
                    onScroll={isSmallScreen ? onScroll : undefined}
                    height={height}
                    itemCount={items.length}
                    itemSize={getItemSize}
                    width={width}
                    outerElementType={isSmallScreen ? undefined : ScrollerWrapper}
                  >
                    {rowRenderer}
                  </VariableSizeList>}
              </Scroller>
            </Measure>
          </>
        );
      }}
    </ReactWindowScroller>
  );
}

VirtualTable.defaultProps = {
  focusScroller: true
};

VirtualTable.propTypes = {
  className: PropTypes.string.isRequired,
  focusScroller: PropTypes.func.isRequired,
  scroller: PropTypes.func.isRequired,
  items: PropTypes.array.isRequired,
  rowRenderer: PropTypes.func.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  columns: PropTypes.array.isRequired,
  sortKey: PropTypes.string.isRequired,
  sortDirection: PropTypes.string.isRequired,
  onSortPress: PropTypes.func.isRequired
};

export default VirtualTable;
