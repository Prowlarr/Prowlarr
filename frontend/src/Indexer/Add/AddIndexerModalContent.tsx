import { some } from 'lodash';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import IndexerAppState from 'App/State/IndexerAppState';
import Alert from 'Components/Alert';
import EnhancedSelectInput from 'Components/Form/EnhancedSelectInput';
import NewznabCategorySelectInputConnector from 'Components/Form/NewznabCategorySelectInputConnector';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Scroller from 'Components/Scroller/Scroller';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds, scrollDirections } from 'Helpers/Props';
import Indexer, { IndexerCategory } from 'Indexer/Indexer';
import {
  fetchIndexerSchema,
  selectIndexerSchema,
  setIndexerSchemaSort,
} from 'Store/Actions/indexerActions';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import { SortCallback } from 'typings/callbacks';
import sortByProp from 'Utilities/Array/sortByProp';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import SelectIndexerRow from './SelectIndexerRow';
import styles from './AddIndexerModalContent.css';

const COLUMNS = [
  {
    name: 'protocol',
    label: () => translate('Protocol'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'sortName',
    label: () => translate('Name'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'language',
    label: () => translate('Language'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'description',
    label: () => translate('Description'),
    isSortable: false,
    isVisible: true,
  },
  {
    name: 'privacy',
    label: () => translate('Privacy'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'categories',
    label: () => translate('Categories'),
    isSortable: false,
    isVisible: true,
  },
];

const PROTOCOLS = [
  {
    key: 'torrent',
    value: 'torrent',
  },
  {
    key: 'usenet',
    value: 'nzb',
  },
];

const PRIVACY_LEVELS = [
  {
    key: 'private',
    get value() {
      return translate('Private');
    },
  },
  {
    key: 'semiPrivate',
    get value() {
      return translate('SemiPrivate');
    },
  },
  {
    key: 'public',
    get value() {
      return translate('Public');
    },
  },
];

interface IndexerSchema extends Indexer {
  isExistingIndexer: boolean;
}

function createAddIndexersSelector() {
  return createSelector(
    createClientSideCollectionSelector('indexers.schema'),
    createAllIndexersSelector(),
    (indexers: IndexerAppState, allIndexers) => {
      const { isFetching, isPopulated, error, items, sortDirection, sortKey } =
        indexers;

      const indexerList: IndexerSchema[] = items.map((item) => {
        const { definitionName } = item;
        return {
          ...item,
          isExistingIndexer: some(allIndexers, { definitionName }),
        };
      });

      return {
        isFetching,
        isPopulated,
        error,
        indexers: indexerList,
        sortKey,
        sortDirection,
      };
    }
  );
}

interface AddIndexerModalContentProps {
  onSelectIndexer(): void;
  onModalClose(): void;
}

function AddIndexerModalContent(props: AddIndexerModalContentProps) {
  const { onSelectIndexer, onModalClose } = props;

  const { isFetching, isPopulated, error, indexers, sortKey, sortDirection } =
    useSelector(createAddIndexersSelector());
  const dispatch = useDispatch();

  const [filter, setFilter] = useState('');
  const [filterProtocols, setFilterProtocols] = useState<string[]>([]);
  const [filterLanguages, setFilterLanguages] = useState<string[]>([]);
  const [filterPrivacyLevels, setFilterPrivacyLevels] = useState<string[]>([]);
  const [filterCategories, setFilterCategories] = useState<number[]>([]);

  useEffect(
    () => {
      dispatch(fetchIndexerSchema());
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    []
  );

  const onFilterChange = useCallback(
    ({ value }: { value: string }) => {
      setFilter(value);
    },
    [setFilter]
  );

  const onFilterProtocolsChange = useCallback(
    ({ value }: { value: string[] }) => {
      setFilterProtocols(value);
    },
    [setFilterProtocols]
  );

  const onFilterLanguagesChange = useCallback(
    ({ value }: { value: string[] }) => {
      setFilterLanguages(value);
    },
    [setFilterLanguages]
  );

  const onFilterPrivacyLevelsChange = useCallback(
    ({ value }: { value: string[] }) => {
      setFilterPrivacyLevels(value);
    },
    [setFilterPrivacyLevels]
  );

  const onFilterCategoriesChange = useCallback(
    ({ value }: { value: number[] }) => {
      setFilterCategories(value);
    },
    [setFilterCategories]
  );

  const onIndexerSelect = useCallback(
    ({
      implementation,
      implementationName,
      name,
    }: {
      implementation: string;
      implementationName: string;
      name: string;
    }) => {
      dispatch(
        selectIndexerSchema({
          implementation,
          implementationName,
          name,
        })
      );

      onSelectIndexer();
    },
    [dispatch, onSelectIndexer]
  );

  const onSortPress = useCallback<SortCallback>(
    (sortKey, sortDirection) => {
      dispatch(setIndexerSchemaSort({ sortKey, sortDirection }));
    },
    [dispatch]
  );

  const languages = useMemo(
    () =>
      Array.from(new Set(indexers.map(({ language }) => language)))
        .map((language) => ({ key: language, value: language }))
        .sort(sortByProp('value')),
    [indexers]
  );

  const filteredIndexers = useMemo(() => {
    const flat = ({
      id,
      subCategories = [],
    }: {
      id: number;
      subCategories: IndexerCategory[];
    }): number[] => [id, ...subCategories.flatMap(flat)];

    return indexers.filter((indexer) => {
      if (
        filter.length &&
        !indexer.name.toLowerCase().includes(filter.toLocaleLowerCase()) &&
        !indexer.description.toLowerCase().includes(filter.toLocaleLowerCase())
      ) {
        return false;
      }

      if (
        filterProtocols.length &&
        !filterProtocols.includes(indexer.protocol)
      ) {
        return false;
      }

      if (
        filterLanguages.length &&
        !filterLanguages.includes(indexer.language)
      ) {
        return false;
      }

      if (
        filterPrivacyLevels.length &&
        !filterPrivacyLevels.includes(indexer.privacy)
      ) {
        return false;
      }

      if (filterCategories.length) {
        const { categories = [] } = indexer.capabilities || {};

        const flatCategories = categories
          .filter((item) => item.id < 100000)
          .flatMap(flat);

        if (
          !filterCategories.every((categoryId) =>
            flatCategories.includes(categoryId)
          )
        ) {
          return false;
        }
      }

      return true;
    });
  }, [
    indexers,
    filter,
    filterProtocols,
    filterLanguages,
    filterPrivacyLevels,
    filterCategories,
  ]);

  const errorMessage = getErrorMessage(
    error,
    translate('UnableToLoadIndexers')
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('AddIndexer')}</ModalHeader>

      <ModalBody
        className={styles.modalBody}
        scrollDirection={scrollDirections.NONE}
      >
        <TextInput
          className={styles.filterInput}
          placeholder={translate('FilterPlaceHolder')}
          name="filter"
          value={filter}
          autoFocus={true}
          onChange={onFilterChange}
        />

        <div className={styles.filterRow}>
          <div className={styles.filterContainer}>
            <label className={styles.filterLabel}>
              {translate('Protocol')}
            </label>

            <EnhancedSelectInput
              name="indexerProtocols"
              value={filterProtocols}
              values={PROTOCOLS}
              onChange={onFilterProtocolsChange}
            />
          </div>

          <div className={styles.filterContainer}>
            <label className={styles.filterLabel}>
              {translate('Language')}
            </label>

            <EnhancedSelectInput
              name="indexerLanguages"
              value={filterLanguages}
              values={languages}
              onChange={onFilterLanguagesChange}
            />
          </div>

          <div className={styles.filterContainer}>
            <label className={styles.filterLabel}>{translate('Privacy')}</label>
            <EnhancedSelectInput
              name="indexerPrivacyLevels"
              value={filterPrivacyLevels}
              values={PRIVACY_LEVELS}
              onChange={onFilterPrivacyLevelsChange}
            />
          </div>

          <div className={styles.filterContainer}>
            <label className={styles.filterLabel}>
              {translate('Categories')}
            </label>

            <NewznabCategorySelectInputConnector
              name="indexerCategories"
              value={filterCategories}
              onChange={onFilterCategoriesChange}
            />
          </div>
        </div>

        <Alert kind={kinds.INFO} className={styles.notice}>
          <div>{translate('ProwlarrSupportsAnyIndexer')}</div>
        </Alert>

        <Scroller className={styles.scroller} autoFocus={false}>
          {isFetching ? <LoadingIndicator /> : null}

          {error ? (
            <Alert kind={kinds.DANGER} className={styles.alert}>
              {errorMessage}
            </Alert>
          ) : null}

          {isPopulated && !!indexers.length ? (
            <Table
              columns={COLUMNS}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
            >
              <TableBody>
                {filteredIndexers.map((indexer) => (
                  <SelectIndexerRow
                    {...indexer}
                    key={`${indexer.implementation}-${indexer.name}`}
                    implementation={indexer.implementation}
                    implementationName={indexer.implementationName}
                    onIndexerSelect={onIndexerSelect}
                  />
                ))}
              </TableBody>
            </Table>
          ) : null}

          {isPopulated && !!indexers.length && !filteredIndexers.length ? (
            <Alert kind={kinds.WARNING} className={styles.alert}>
              {translate('NoIndexersFound')}
            </Alert>
          ) : null}
        </Scroller>
      </ModalBody>

      <ModalFooter className={styles.modalFooter}>
        <div className={styles.available}>
          {isPopulated
            ? translate('CountIndexersAvailable', {
                count: filteredIndexers.length,
              })
            : null}
        </div>

        <div>
          <Button onPress={onModalClose}>{translate('Close')}</Button>
        </div>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddIndexerModalContent;
