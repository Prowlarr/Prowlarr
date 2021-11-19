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
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import { fetchIndexerSchema, SELECT_INDEXER_SCHEMA, SET_INDEXER_SCHEMA_SORT } from '../../Store/Actions/indexerActions';
import createClientSideCollectionSelector from '../../Store/Selectors/createClientSideCollectionSelector';
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

function createMapStateToProps() {
  return createSelector(
    createClientSideCollectionSelector('indexers.schema'),
    (indexers) => {
      const {
        isFetching,
        isPopulated,
        error,
        items,
        sortDirection,
        sortKey
      } = indexers;

      return {
        isFetching,
        isPopulated,
        error,
        indexers: items,
        sortKey,
        sortDirection
      };
    }
  );
}

function AddIndexerModalContent({
  onModalClose
}) {
  //
  // Lifecycle
  const [filter, setFilter] = useState('');
  const dispatch = useDispatch();

  //
  // Data
  const { indexers, isFetching, isPopulated, error, sortKey, sortDirection } = useSelector(createMapStateToProps());

  //
  // Listeners
  useEffect(() => {
    dispatch(fetchIndexerSchema());
  }, []);

  function onSortPress(eventSortKey) {
    dispatch({ type: SET_INDEXER_SCHEMA_SORT, payload: { sortKey: eventSortKey } });
  }

  function onIndexerSelect({ implementation, name }) {
    dispatch({ type: SELECT_INDEXER_SCHEMA, payload: { implementation, name } });
    onModalClose({ indexerSelected: true });
  }

  function onFilterChange({ value }) {
    setFilter(value);
  }

  //
  // Render

  const filterLower = filter.toLowerCase();

  const errorMessage = getErrorMessage(error, 'Unable to load indexers');

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
            isPopulated && !!indexers.length ?
              <Table
                columns={columns}
                sortKey={sortKey}
                sortDirection={sortDirection}
                onSortPress={onSortPress}
              >
                <TableBody>
                  {
                    indexers.map((indexer) => {
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
  onModalClose: PropTypes.func.isRequired
};

export default AddIndexerModalContent;
