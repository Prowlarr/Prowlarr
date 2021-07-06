import PropTypes from 'prop-types';
import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import Alert from 'Components/Alert';
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
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import { SET_FILTERED_INDEXERS } from '../../Store/Actions/indexerActions';
import FilterIndexers from './FilterIndexers';
import SelectIndexerRow from './SelectIndexerRow';
import styles from './AddIndexerModalContent.css';

const columns = [
  {
    name: 'protocol',
    label: translate('Protocol'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'name',
    label: translate('Name'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'language',
    label: translate('Language'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'privacy',
    label: translate('Privacy'),
    isSortable: true,
    isVisible: true
  }
];

function AddIndexerModalContent({
  onIndexerSelect,
  sortKey,
  sortDirection,
  isFetching,
  isPopulated,
  error,
  onSortPress,
  onModalClose
}) {
  const [filter, setFilter] = useState('');
  const { filteredIndexers, externalIndexers } = useSelector(createSelector(createClientSideCollectionSelector('indexers.schema', undefined, 'filteredIndexers'),
    (schema) => {
      const {
        filteredIndexers: internalFilteredIndexers,
        items
      } = schema;
      return {
        filteredIndexers: internalFilteredIndexers,
        externalIndexers: items
      };
    }));
  const { isExtraSmallScreen } = useSelector((state) => state.app.dimensions);
  const dispatch = useDispatch();

  function setFilteredIndexers(param) {
    dispatch({ type: SET_FILTERED_INDEXERS, payload: param.map((indexer) => {
      return indexer.name;
    }) });
  }

  function setIndexersWrapper(param) {
    setFilteredIndexers(param);
  }

  useEffect(() => {
    if (externalIndexers.length > 0 && filteredIndexers.length === 0) {
      setFilteredIndexers(externalIndexers);
    }
  }, [externalIndexers]);

  //
  // Listeners

  function onFilterChange({ value }) {
    setFilter(value);
  }

  const filterLower = filter.toLowerCase();

  const errorMessage = getErrorMessage(error, 'Unable to load indexers');
  const secondaryHeaderRow = isExtraSmallScreen ? null : <FilterIndexers indexers={externalIndexers} setIndexers={setIndexersWrapper} />;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        Add Indexer
      </ModalHeader>

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

        <Alert
          kind={kinds.INFO}
          className={styles.alert}
        >
          <div>
            {translate('ProwlarrSupportsAnyIndexer')}
          </div>
        </Alert>

        <Scroller
          className={styles.scroller}
          autoFocus={false}
        >
          {
            isFetching ? <LoadingIndicator /> : null
          }
          {
            error ? <div>{errorMessage}</div> : null
          }
          {
            isPopulated ?
              <Table
                columns={columns}
                sortKey={sortKey}
                sortDirection={sortDirection}
                onSortPress={onSortPress}
                secondaryHeaderRow={secondaryHeaderRow}
              >
                <TableBody>
                  {
                    externalIndexers.map((indexer) => {
                      if (!filteredIndexers.includes(indexer.name)) {
                        return null;
                      }
                      return indexer.name.toLowerCase().includes(filterLower) ?
                        (
                          <SelectIndexerRow
                            key={indexer.name}
                            implementation={indexer.implementation}
                            {...indexer}
                            onIndexerSelect={onIndexerSelect}
                          />
                        ) :
                        null;
                    })
                  }
                </TableBody>
              </Table> :
              null
          }
          {
            (filteredIndexers.length === 0 && !isFetching && !error) &&
              <Alert
                kind={kinds.WARNING}
                className={styles.alert}
              >
                <div>
                  {translate('NoResultsFound')}
                </div>
              </Alert>
          }
        </Scroller>
      </ModalBody>

      <ModalFooter>
        <Button
          onPress={onModalClose}
        >
          {translate('Close')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

AddIndexerModalContent.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.string,
  onSortPress: PropTypes.func.isRequired,
  indexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  onIndexerSelect: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddIndexerModalContent;
