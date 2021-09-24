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
import IndexerEditorFooter from 'Indexer/Editor/IndexerEditorFooter.js';
import NoIndexer from 'Indexer/NoIndexer';
import * as keyCodes from 'Utilities/Constants/keyCodes';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import selectAll from 'Utilities/Table/selectAll';
import toggleSelected from 'Utilities/Table/toggleSelected';
import IndexerIndexFooterConnector from './IndexerIndexFooterConnector';
import IndexerIndexFilterMenu from './Menus/IndexerIndexFilterMenu';
import IndexerIndexSortMenu from './Menus/IndexerIndexSortMenu';
import IndexerIndexTableConnector from './Table/IndexerIndexTableConnector';
import IndexerIndexTableOptionsConnector from './Table/IndexerIndexTableOptionsConnector';
import styles from './IndexerIndex.css';

function getViewComponent() {
  return IndexerIndexTableConnector;
}

class IndexerIndex extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scroller: null,
      jumpBarItems: { order: [] },
      jumpToCharacter: null,
      isMovieEditorActive: false,
      isAddIndexerModalOpen: false,
      isEditIndexerModalOpen: false,
      searchType: null,
      allSelected: false,
      allUnselected: false,
      lastToggled: null,
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
      sortDirection,
      isDeleting,
      deleteError
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

    const hasFinishedDeleting = prevProps.isDeleting &&
                                !isDeleting &&
                                !deleteError;

    if (hasFinishedDeleting) {
      this.onSelectAllChange({ value: false });
    }
  }

  //
  // Control

  setScrollerRef = (ref) => {
    this.setState({ scroller: ref });
  }

  getSelectedIds = () => {
    if (this.state.allUnselected) {
      return [];
    }
    return getSelectedIds(this.state.selectedState);
  }

  setSelectedState() {
    const {
      items
    } = this.props;

    const {
      selectedState
    } = this.state;

    const newSelectedState = {};

    items.forEach((movie) => {
      const isItemSelected = selectedState[movie.id];

      if (isItemSelected) {
        newSelectedState[movie.id] = isItemSelected;
      } else {
        newSelectedState[movie.id] = false;
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
    if (sortKey !== 'sortTitle') {
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

  onMovieEditorTogglePress = () => {
    if (this.state.isMovieEditorActive) {
      this.setState({ isMovieEditorActive: false });
    } else {
      const newState = selectAll(this.state.selectedState, false);
      newState.isMovieEditorActive = true;
      this.setState(newState);
    }
  }

  onJumpBarItemPress = (jumpToCharacter) => {
    this.setState({ jumpToCharacter });
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

  onSelectAllChange = ({ value }) => {
    this.setState(selectAll(this.state.selectedState, value));
  }

  onSelectAllPress = () => {
    this.onSelectAllChange({ value: !this.state.allSelected });
  }

  onSelectedChange = ({ id, value, shiftKey = false }) => {
    this.setState((state) => {
      return toggleSelected(state, this.props.items, id, value, shiftKey);
    });
  }

  onSaveSelected = (changes) => {
    this.props.onSaveSelected({
      indexerIds: this.getSelectedIds(),
      ...changes
    });
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
      isSaving,
      saveError,
      isDeleting,
      isTestingAll,
      deleteError,
      onScroll,
      onSortSelect,
      onFilterSelect,
      onTestAllPress,
      ...otherProps
    } = this.props;

    const {
      scroller,
      jumpBarItems,
      jumpToCharacter,
      isAddIndexerModalOpen,
      isEditIndexerModalOpen,
      isMovieEditorActive,
      selectedState,
      allSelected,
      allUnselected
    } = this.state;

    const selectedIndexerIds = this.getSelectedIds();

    const ViewComponent = getViewComponent();
    const isLoaded = !!(!error && isPopulated && items.length && scroller);
    const hasNoIndexer = !totalItems;

    return (
      <PageContent>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={'Add Indexer'}
              iconName={icons.ADD}
              spinningName={icons.ADD}
              onPress={this.onAddIndexerPress}
            />

            <PageToolbarButton
              label={'Test All Indexers'}
              iconName={icons.TEST}
              isSpinning={isTestingAll}
              isDisabled={hasNoIndexer}
              onPress={this.props.onTestAllPress}
            />

            <PageToolbarSeparator />

            {
              isMovieEditorActive ?
                <PageToolbarButton
                  label={'Indexers'}
                  iconName={icons.MOVIE_CONTINUING}
                  isDisabled={hasNoIndexer}
                  onPress={this.onMovieEditorTogglePress}
                /> :
                <PageToolbarButton
                  label={'Mass Editor'}
                  iconName={icons.EDIT}
                  isDisabled={hasNoIndexer}
                  onPress={this.onMovieEditorTogglePress}
                />
            }

            {
              isMovieEditorActive ?
                <PageToolbarButton
                  label={allSelected ? translate('UnselectAll') : translate('SelectAll')}
                  iconName={icons.CHECK_SQUARE}
                  isDisabled={hasNoIndexer}
                  onPress={this.onSelectAllPress}
                /> :
                null
            }

          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >

            <TableOptionsModalWrapper
              {...otherProps}
              columns={columns}
              optionsComponent={IndexerIndexTableOptionsConnector}
            >
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper> :
            null

            <PageToolbarSeparator />

            <IndexerIndexSortMenu
              sortKey={sortKey}
              sortDirection={sortDirection}
              isDisabled={hasNoIndexer}
              onSortSelect={onSortSelect}
            />

            <IndexerIndexFilterMenu
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
                  {getErrorMessage(error, 'Failed to load indexers from API')}
                </div>
            }

            {
              isLoaded &&
                <div className={styles.contentBodyContainer}>
                  <ViewComponent
                    scroller={scroller}
                    items={items}
                    filters={filters}
                    sortKey={sortKey}
                    sortDirection={sortDirection}
                    jumpToCharacter={jumpToCharacter}
                    isMovieEditorActive={isMovieEditorActive}
                    allSelected={allSelected}
                    allUnselected={allUnselected}
                    onSelectedChange={this.onSelectedChange}
                    onSelectAllChange={this.onSelectAllChange}
                    selectedState={selectedState}
                    {...otherProps}
                  />

                  {
                    !isMovieEditorActive &&
                      <IndexerIndexFooterConnector />
                  }
                </div>
            }

            {
              !error && isPopulated && !items.length &&
                <NoIndexer
                  totalItems={totalItems}
                  onAddIndexerPress={this.onAddIndexerPress}
                />
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

        {
          isLoaded && isMovieEditorActive &&
            <IndexerEditorFooter
              indexerIds={selectedIndexerIds}
              selectedCount={selectedIndexerIds.length}
              isSaving={isSaving}
              saveError={saveError}
              isDeleting={isDeleting}
              deleteError={deleteError}
              onSaveSelected={this.onSaveSelected}
              onOrganizeMoviePress={this.onOrganizeMoviePress}
            />
        }

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

IndexerIndex.propTypes = {
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
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  isTestingAll: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSortSelect: PropTypes.func.isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  onTestAllPress: PropTypes.func.isRequired,
  onScroll: PropTypes.func.isRequired,
  onSaveSelected: PropTypes.func.isRequired
};

export default IndexerIndex;
