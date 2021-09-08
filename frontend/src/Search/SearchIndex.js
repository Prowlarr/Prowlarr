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
import AddIndexerModal from 'Indexer/Add/AddIndexerModal';
import EditIndexerModalConnector from 'Indexer/Edit/EditIndexerModalConnector';
import NoIndexer from 'Indexer/NoIndexer';
import * as keyCodes from 'Utilities/Constants/keyCodes';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import SearchIndexFilterMenu from './Menus/SearchIndexFilterMenu';
import SearchIndexSortMenu from './Menus/SearchIndexSortMenu';
import NoSearchResults from './NoSearchResults';
import SearchFooter from './SearchFooter';
import SearchVirtualTable from './Table/SearchVirtualTable';
import styles from './SearchIndex.css';

class SearchIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isAddIndexerModalOpen: false,
      isEditIndexerModalOpen: false,
      searchType: null,
      lastToggled: null
    };
  }

  componentDidMount() {
    this.setJumpBarItems();

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
    }

    if (this.state.jumpToCharacter != null) {
      this.setState({ jumpToCharacter: null });
    }
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
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

  onAddIndexerPress = () => {
    this.setState({ isAddIndexerModalOpen: true });
  }

  onAddIndexerModalClose = ({ indexerSelected = false } = {}) => {
    this.setState({
      isAddIndexerModalOpen: false,
      isEditIndexerModalOpen: indexerSelected
    });
  }

  onEditIndexerModalClose = () => {
    this.setState({ isEditIndexerModalOpen: false });
  }

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
  }

  onSearchPress = (query, indexerIds, categories) => {
    this.props.onSearchPress({ query, indexerIds, categories });
  }

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
  }

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      error,
      totalItems,
      items,
      columns,
      selectedFilterKey,
      filters,
      customFilters,
      sortKey,
      sortDirection,
      onScroll,
      onSortSelect,
      onFilterSelect,
      hasIndexers,
      ...otherProps
    } = this.props;

    const {
      scroller,
      jumpBarItems,
      isAddIndexerModalOpen,
      isEditIndexerModalOpen,
      jumpToCharacter
    } = this.state;

    const isLoaded = !!(!error && isPopulated && items.length && scroller);
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
            registerScroller={this.setScrollerRef}
            className={styles.contentBody}
            innerClassName={styles.tableInnerContentBody}
            onScroll={onScroll}
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
                  <SearchVirtualTable
                    items={items}
                    jumpToCharacter={jumpToCharacter}
                    columns={columns}
                    scroller={scroller}
                    sortDirection={sortDirection}
                    sortKey={sortKey}
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

        <SearchFooter
          isFetching={isFetching}
          hasIndexers={hasIndexers}
          onSearchPress={this.onSearchPress}
        />

        <AddIndexerModal
          isOpen={isAddIndexerModalOpen}
          onModalClose={this.onAddIndexerModalClose}
        />

        <EditIndexerModalConnector
          isOpen={isEditIndexerModalOpen}
          onModalClose={this.onEditIndexerModalClose}
        />
      </PageContent>
    );
  }
}

SearchIndex.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
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
  onScroll: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired
};

export default SearchIndex;
