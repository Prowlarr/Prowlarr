import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import { align, icons, sortDirections } from 'Helpers/Props';
import NoIndexer from 'Indexer/NoIndexer';
import * as keyCodes from 'Utilities/Constants/keyCodes';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import SearchIndexFilterMenu from './Menus/SearchIndexFilterMenu';
import SearchIndexSortMenu from './Menus/SearchIndexSortMenu';
import SearchIndexOverviewsConnector from './Mobile/SearchIndexOverviewsConnector';
import NoSearchResults from './NoSearchResults';
import SearchFooterConnector from './SearchFooterConnector';
import SearchIndexTableConnector from './Table/SearchIndexTableConnector';
import styles from './SearchIndex.css';

function getViewComponent(isSmallScreen) {
  if (isSmallScreen) {
    return SearchIndexOverviewsConnector;
  }

  return SearchIndexTableConnector;
}

class SearchIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.scrollerRef = React.createRef();

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      searchType: null,
      lastToggled: null,
      allSelected: false,
      allUnselected: false,
      selectedState: {}
    };
  }

  componentDidMount() {
    this.setJumpBarItems();
    this.setSelectedState();

    window.addEventListener('keyup', this.onKeyUp);
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    if (sortKey !== prevProps.sortKey ||
        sortDirection !== prevProps.sortDirection ||
        hasDifferentItemsOrOrder(prevProps.items, items)
    ) {
      this.setJumpBarItems();
      this.setSelectedState();
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  getSelectedIds = () => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState, { parseIds: false });
  };

  setSelectedState() {
    const {
      items
    } = this.props;

    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((release) => {
      const isItemSelected = selectedState[release.guid];

      if (isItemSelected) {
        newSelectedState[release.guid] = isItemSelected;
      } else {
        newSelectedState[release.guid] = false;
      }
    });

    const selectedCount = getSelectedIds(newSelectedState).length;
    const newStateCount = Object.keys(newSelectedState).length;
    let isAllSelected = false;
    let isAllUnselected = false;

    if (selectedCount === 0) {
      isAllUnselected = true;
    } else if (selectedCount === newStateCount) {
      isAllSelected = true;
    }

    this.setState({ selectedState: newSelectedState, allSelected: isAllSelected, allUnselected: isAllUnselected });
  }

  setJumpBarItems() {
    const {
      items,
      sortKey,
      sortDirection
    } = this.props;

    // Reset if not sorting by sortTitle
    if (sortKey !== 'title') {
      this.setState({ jumpBarItems: { order: [] } });
      return;
    }

    const characters = _.reduce(items, (acc, item) => {
      let char = item.sortTitle.charAt(0);

      if (!isNaN(char)) {
        char = '#';
      }

      if (char in acc) {
        acc[char] = acc[char] + 1;
      } else {
        acc[char] = 1;
      }

      return acc;
    }, {});

    const order = Object.keys(characters).sort();

    // Reverse if sorting descending
    if (sortDirection === sortDirections.DESCENDING) {
      order.reverse();
    }

    const jumpBarItems = {
      characters,
      order
    };

    this.setState({ jumpBarItems });
  }

  //
  // Listeners

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  };

  onSearchPress = (query, indexerIds, categories, type, limit, offset) => {
    this.props.onSearchPress({ query, indexerIds, categories, type, limit, offset });
  };

  onBulkGrabPress = () => {
    const selectedIds = this.getSelectedIds();
    const result = _.filter(this.props.items, (release) => _.indexOf(selectedIds, release.guid) !== -1);
    this.props.onBulkGrabPress(result);
  };

  onKeyUp = (event) => {
    const jumpBarItems = this.state.jumpBarItems.order;
    if (event.path.length === 4) {
      if (event.keyCode === keyCodes.HOME && event.ctrlKey) {
        this.setState({ jumpToCharacter: jumpBarItems[0] });
      }
      if (event.keyCode === keyCodes.END && event.ctrlKey) {
        this.setState({ jumpToCharacter: jumpBarItems[jumpBarItems.length - 1] });
      }
    }
  };

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  };

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  };

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      isGrabbing,
      error,
      grabError,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      onSortSelect,
      onFilterSelect,
      isSmallScreen,
      hasIndexers,
      ...otherProps
    } = this.props;

    const {
      jumpBarItems,
      jumpToCharacter,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedIndexerIds = this.getSelectedIds();

    const ViewComponent = getViewComponent(isSmallScreen);
    const isLoaded = !!(!error && isPopulated && items.length && this.scrollerRef.current);
    const hasNoIndexer = !totalItems;

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            <TableOptionsModalWrapper
              {...otherProps}
              columns={columns}
            >
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

            <PageToolbarSeparator />

            <SearchIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoIndexer}
              onSortSelect={onSortSelect}
            />

            <SearchIndexFilterMenu
              selectedFilterKey={selectedFilterKey}
              filters={filters}
              customFilters={customFilters}
              isDisabled={hasNoIndexer}
              onFilterSelect={onFilterSelect}
            />
          </PageToolbarSection>
        </PageToolbar>

        <div className={styles.pageContentBodyWrapper}>
          <PageContentBody
            ref={this.scrollerRef}
            className={styles.contentBody}
            innerClassName={styles.tableInnerContentBody}
          >
            {
              isFetching && !isPopulated &&
                <LoadingIndicator />
            }

            {
              !isFetching && !!error &&
                <div className={styles.errorMessage}>
                  {getErrorMessage(error, 'Failed to load search results from API')}
                </div>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={this.scrollerRef.current}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    columns={columns}
                    jumpToCharacter={jumpToCharacter}
                    allSelected={allSelected}
                    allUnselected={allUnselected}
                    onSelectedChange={this.onSelectedChange}
                    onSelectAllChange={this.onSelectAllChange}
                    selectedState={selectedState}
                    {...otherProps}
                  />
                </div>
            }

            {
              !error && !isFetching && !hasIndexers &&
                <NoIndexer
                  totalItems={0}
                  onAddIndexerPress={this.onAddIndexerPress}
                />
            }

            {
              !error && !isFetching && hasIndexers && !items.length &&
                <NoSearchResults totalItems={totalItems} />
            }
          </PageContentBody>

          {
            isLoaded && !!jumpBarItems.order.length &&
              <PageJumpBar
                items={jumpBarItems}
                onItemPress={this.onJumpBarItemPress}
              />
          }
        </div>

        <SearchFooterConnector
          isFetching={isFetching}
          isPopulated={isPopulated}
          isGrabbing={isGrabbing}
          grabError={grabError}
          selectedCount={selectedIndexerIds.length}
          itemCount={items.length}
          hasIndexers={hasIndexers}
          onSearchPress={this.onSearchPress}
          onBulkGrabPress={this.onBulkGrabPress}
        />
      </PageContent>
    );
  }
}

SearchIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  isGrabbing: PropTypes.bool.isRequired,
  error: PropTypes.object,
  grabError: PropTypes.object,
  totalItems: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  isSmallScreen: PropTypes.bool.isRequired,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  onBulkGrabPress: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired
};

export default SearchIndex;
