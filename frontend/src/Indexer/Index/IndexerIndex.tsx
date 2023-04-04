import React, { useCallback, useMemo, useRef, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { SelectProvider } from 'App/SelectContext';
import ClientSideCollectionAppState from 'App/State/ClientSideCollectionAppState';
import IndexerAppState, {
  IndexerIndexAppState,
} from 'App/State/IndexerAppState';
import { APP_INDEXER_SYNC } from 'Commands/commandNames';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageJumpBar from 'Components/Page/PageJumpBar';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import withScrollPosition from 'Components/withScrollPosition';
import { align, icons } from 'Helpers/Props';
import SortDirection from 'Helpers/Props/SortDirection';
import AddIndexerModal from 'Indexer/Add/AddIndexerModal';
import EditIndexerModalConnector from 'Indexer/Edit/EditIndexerModalConnector';
import NoIndexer from 'Indexer/NoIndexer';
import { executeCommand } from 'Store/Actions/commandActions';
import { testAllIndexers } from 'Store/Actions/indexerActions';
import {
  setIndexerFilter,
  setIndexerSort,
  setIndexerTableOption,
} from 'Store/Actions/indexerIndexActions';
import scrollPositions from 'Store/scrollPositions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createIndexerClientSideCollectionItemsSelector from 'Store/Selectors/createIndexerClientSideCollectionItemsSelector';
import translate from 'Utilities/String/translate';
import IndexerIndexFooter from './IndexerIndexFooter';
import IndexerIndexFilterMenu from './Menus/IndexerIndexFilterMenu';
import IndexerIndexSortMenu from './Menus/IndexerIndexSortMenu';
import IndexerIndexSelectAllButton from './Select/IndexerIndexSelectAllButton';
import IndexerIndexSelectAllMenuItem from './Select/IndexerIndexSelectAllMenuItem';
import IndexerIndexSelectFooter from './Select/IndexerIndexSelectFooter';
import IndexerIndexSelectModeButton from './Select/IndexerIndexSelectModeButton';
import IndexerIndexSelectModeMenuItem from './Select/IndexerIndexSelectModeMenuItem';
import IndexerIndexTable from './Table/IndexerIndexTable';
import IndexerIndexTableOptions from './Table/IndexerIndexTableOptions';
import styles from './IndexerIndex.css';

function getViewComponent() {
  return IndexerIndexTable;
}

interface IndexerIndexProps {
  initialScrollTop?: number;
}

const IndexerIndex = withScrollPosition((props: IndexerIndexProps) => {
  const {
    isFetching,
    isPopulated,
    isTestingAll,
    error,
    totalItems,
    items,
    columns,
    selectedFilterKey,
    filters,
    customFilters,
    sortKey,
    sortDirection,
    view,
  }: IndexerAppState & IndexerIndexAppState & ClientSideCollectionAppState =
    useSelector(createIndexerClientSideCollectionItemsSelector('indexerIndex'));

  const isSyncingIndexers = useSelector(
    createCommandExecutingSelector(APP_INDEXER_SYNC)
  );
  const { isSmallScreen } = useSelector(createDimensionsSelector());
  const dispatch = useDispatch();
  const scrollerRef = useRef<HTMLDivElement>(null);
  const [isAddIndexerModalOpen, setIsAddIndexerModalOpen] = useState(false);
  const [isEditIndexerModalOpen, setIsEditIndexerModalOpen] = useState(false);
  const [jumpToCharacter, setJumpToCharacter] = useState<string | undefined>(
    undefined
  );
  const [isSelectMode, setIsSelectMode] = useState(false);

  const onAppIndexerSyncPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: APP_INDEXER_SYNC,
      })
    );
  }, [dispatch]);

  const onAddIndexerPress = useCallback(() => {
    setIsAddIndexerModalOpen(true);
  }, [setIsAddIndexerModalOpen]);

  const onAddIndexerModalClose = useCallback(() => {
    setIsAddIndexerModalOpen(false);
  }, [setIsAddIndexerModalOpen]);

  const onEditIndexerPress = useCallback(() => {
    setIsEditIndexerModalOpen(true);
  }, [setIsEditIndexerModalOpen]);

  const onEditIndexerModalClose = useCallback(() => {
    setIsEditIndexerModalOpen(false);
  }, [setIsEditIndexerModalOpen]);

  const onTestAllPress = useCallback(() => {
    dispatch(testAllIndexers());
  }, [dispatch]);

  const onSelectModePress = useCallback(() => {
    setIsSelectMode(!isSelectMode);
  }, [isSelectMode, setIsSelectMode]);

  const onTableOptionChange = useCallback(
    (payload: unknown) => {
      dispatch(setIndexerTableOption(payload));
    },
    [dispatch]
  );

  const onSortSelect = useCallback(
    (value: string) => {
      dispatch(setIndexerSort({ sortKey: value }));
    },
    [dispatch]
  );

  const onFilterSelect = useCallback(
    (value: string) => {
      dispatch(setIndexerFilter({ selectedFilterKey: value }));
    },
    [dispatch]
  );

  const onJumpBarItemPress = useCallback(
    (character: string) => {
      setJumpToCharacter(character);
    },
    [setJumpToCharacter]
  );

  const onScroll = useCallback(
    ({ scrollTop }: { scrollTop: number }) => {
      setJumpToCharacter(undefined);
      scrollPositions.indexerIndex = scrollTop;
    },
    [setJumpToCharacter]
  );

  const jumpBarItems = useMemo(() => {
    // Reset if not sorting by sortName
    if (sortKey !== 'sortName') {
      return {
        order: [],
      };
    }

    const characters = items.reduce((acc: Record<string, number>, item) => {
      let char = item.sortName.charAt(0);

      if (!isNaN(Number(char))) {
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
    if (sortDirection === SortDirection.Descending) {
      order.reverse();
    }

    return {
      characters,
      order,
    };
  }, [items, sortKey, sortDirection]);
  const ViewComponent = useMemo(() => getViewComponent(), []);

  const isLoaded = !!(!error && isPopulated && items.length);
  const hasNoIndexer = !totalItems;

  return (
    <SelectProvider items={items}>
      <PageContent title={translate('Indexers')}>
        <PageToolbar>
          <PageToolbarSection>
            <PageToolbarButton
              label={translate('AddIndexer')}
              iconName={icons.ADD}
              spinningName={icons.ADD}
              onPress={onAddIndexerPress}
            />

            <PageToolbarSeparator />

            <PageToolbarButton
              label={translate('SyncAppIndexers')}
              iconName={icons.REFRESH}
              spinningName={icons.REFRESH}
              isSpinning={isSyncingIndexers}
              isDisabled={hasNoIndexer}
              onPress={onAppIndexerSyncPress}
            />

            <PageToolbarButton
              label={translate('TestAllIndexers')}
              iconName={icons.TEST}
              isSpinning={isTestingAll}
              isDisabled={hasNoIndexer}
              onPress={onTestAllPress}
            />

            <PageToolbarSeparator />

            <IndexerIndexSelectModeButton
              label={
                isSelectMode
                  ? translate('StopSelecting')
                  : translate('SelectIndexers')
              }
              iconName={isSelectMode ? icons.SERIES_ENDED : icons.CHECK}
              isSelectMode={isSelectMode}
              overflowComponent={IndexerIndexSelectModeMenuItem}
              onPress={onSelectModePress}
            />

            <IndexerIndexSelectAllButton
              label={translate('SelectAll')}
              isSelectMode={isSelectMode}
              overflowComponent={IndexerIndexSelectAllMenuItem}
            />
          </PageToolbarSection>

          <PageToolbarSection
            alignContent={align.RIGHT}
            collapseButtons={false}
          >
            <TableOptionsModalWrapper
              columns={columns}
              optionsComponent={IndexerIndexTableOptions}
              onTableOptionChange={onTableOptionChange}
            >
              <PageToolbarButton
                label={translate('Options')}
                iconName={icons.TABLE}
              />
            </TableOptionsModalWrapper>

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
            ref={scrollerRef}
            className={styles.contentBody}
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            innerClassName={styles[`${view}InnerContentBody`]}
            initialScrollTop={props.initialScrollTop}
            onScroll={onScroll}
          >
            {isFetching && !isPopulated ? <LoadingIndicator /> : null}

            {!isFetching && !!error ? (
              <div>{translate('UnableToLoadIndexers')}</div>
            ) : null}

            {isLoaded ? (
              <div className={styles.contentBodyContainer}>
                <ViewComponent
                  scrollerRef={scrollerRef}
                  items={items}
                  sortKey={sortKey}
                  sortDirection={sortDirection}
                  jumpToCharacter={jumpToCharacter}
                  isSelectMode={isSelectMode}
                  isSmallScreen={isSmallScreen}
                />

                <IndexerIndexFooter />
              </div>
            ) : null}

            {!error && isPopulated && !items.length ? (
              <NoIndexer
                totalItems={totalItems}
                onAddIndexerPress={onAddIndexerPress}
              />
            ) : null}
          </PageContentBody>
          {isLoaded && !!jumpBarItems.order.length ? (
            <PageJumpBar
              items={jumpBarItems}
              onItemPress={onJumpBarItemPress}
            />
          ) : null}
        </div>

        {isSelectMode ? <IndexerIndexSelectFooter /> : null}

        <AddIndexerModal
          isOpen={isAddIndexerModalOpen}
          onModalClose={onAddIndexerModalClose}
          onSelectIndexer={onEditIndexerPress}
        />

        <EditIndexerModalConnector
          isOpen={isEditIndexerModalOpen}
          onModalClose={onEditIndexerModalClose}
        />
      </PageContent>
    </SelectProvider>
  );
}, 'indexerIndex');

export default IndexerIndex;
