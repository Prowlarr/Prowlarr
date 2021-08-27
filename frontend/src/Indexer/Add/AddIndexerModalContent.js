import partition from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
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

class AddIndexerModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      filter: ''
    };
  }

  //
  // Listeners

  onFilterChange = ({ value }) => {
    this.setState({ filter: value });
  }

  //
  // Render

  render() {
    const {
      indexers,
      onIndexerSelect,
      sortKey,
      sortDirection,
      isFetching,
      isPopulated,
      error,
      onSortPress,
      onModalClose
    } = this.props;

    const filter = this.state.filter;
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
            onChange={this.onFilterChange}
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
                      partition(indexers, (indexer) => indexer.pinned)
                        .flatMap((partedIndexers) =>
                          partedIndexers.map((indexer) => {
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
                        )
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
