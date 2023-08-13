import PropTypes from 'prop-types';
import React, { Component } from 'react';
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
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import SelectIndexerRowConnector from './SelectIndexerRowConnector';
import styles from './AddIndexerModalContent.css';

const columns = [
  {
    name: 'protocol',
    label: () => translate('Protocol'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'sortName',
    label: () => translate('Name'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'language',
    label: () => translate('Language'),
    isSortable: true,
    isVisible: true
  },
  {
    name: 'description',
    label: () => translate('Description'),
    isSortable: false,
    isVisible: true
  },
  {
    name: 'privacy',
    label: () => translate('Privacy'),
    isSortable: true,
    isVisible: true
  }
];

const protocols = [
  {
    key: 'torrent',
    value: 'torrent'
  },
  {
    key: 'usenet',
    value: 'nzb'
  }
];

const privacyLevels = [
  {
    key: 'private',
    get value() {
      return translate('Private');
    }
  },
  {
    key: 'semiPrivate',
    get value() {
      return translate('SemiPrivate');
    }
  },
  {
    key: 'public',
    get value() {
      return translate('Public');
    }
  }
];

class AddIndexerModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      filter: '',
      filterProtocols: [],
      filterLanguages: [],
      filterPrivacyLevels: [],
      filterCategories: []
    };
  }

  //
  // Listeners

  onFilterChange = ({ value }) => {
    this.setState({ filter: value });
  };

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

    const languages = Array.from(new Set(indexers.map(({ language }) => language)))
      .sort((a, b) => a.localeCompare(b))
      .map((language) => ({ key: language, value: language }));

    const filteredIndexers = indexers.filter((indexer) => {
      const {
        filter,
        filterProtocols,
        filterLanguages,
        filterPrivacyLevels,
        filterCategories
      } = this.state;

      if (!indexer.name.toLowerCase().includes(filter.toLocaleLowerCase()) && !indexer.description.toLowerCase().includes(filter.toLocaleLowerCase())) {
        return false;
      }

      if (filterProtocols.length && !filterProtocols.includes(indexer.protocol)) {
        return false;
      }

      if (filterLanguages.length && !filterLanguages.includes(indexer.language)) {
        return false;
      }

      if (filterPrivacyLevels.length && !filterPrivacyLevels.includes(indexer.privacy)) {
        return false;
      }

      if (filterCategories.length) {
        const { categories = [] } = indexer.capabilities || {};
        const flat = ({ id, subCategories = [] }) => [id, ...subCategories.flatMap(flat)];
        const flatCategories = categories
          .filter((item) => item.id < 100000)
          .flatMap(flat);

        if (!filterCategories.every((item) => flatCategories.includes(item))) {
          return false;
        }
      }

      return true;
    });

    const errorMessage = getErrorMessage(error, translate('UnableToLoadIndexers'));

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddIndexer')}
        </ModalHeader>

        <ModalBody
          className={styles.modalBody}
          scrollDirection={scrollDirections.NONE}
        >
          <TextInput
            className={styles.filterInput}
            placeholder={translate('FilterPlaceHolder')}
            name="filter"
            value={this.state.filter}
            autoFocus={true}
            onChange={this.onFilterChange}
          />

          <div className={styles.filterRow}>
            <div className={styles.filterContainer}>
              <label className={styles.filterLabel}>{translate('Protocol')}</label>
              <EnhancedSelectInput
                name="indexerProtocols"
                value={this.state.filterProtocols}
                values={protocols}
                onChange={({ value }) => this.setState({ filterProtocols: value })}
              />
            </div>

            <div className={styles.filterContainer}>
              <label className={styles.filterLabel}>{translate('Language')}</label>
              <EnhancedSelectInput
                name="indexerLanguages"
                value={this.state.filterLanguages}
                values={languages}
                onChange={({ value }) => this.setState({ filterLanguages: value })}
              />
            </div>

            <div className={styles.filterContainer}>
              <label className={styles.filterLabel}>{translate('Privacy')}</label>
              <EnhancedSelectInput
                name="indexerPrivacyLevels"
                value={this.state.filterPrivacyLevels}
                values={privacyLevels}
                onChange={({ value }) => this.setState({ filterPrivacyLevels: value })}
              />
            </div>

            <div className={styles.filterContainer}>
              <label className={styles.filterLabel}>{translate('Categories')}</label>
              <NewznabCategorySelectInputConnector
                name="indexerCategories"
                value={this.state.filterCategories}
                onChange={({ value }) => this.setState({ filterCategories: value })}
              />
            </div>
          </div>

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
              error ? <Alert kind={kinds.DANGER}>{errorMessage}</Alert> : null
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
                      filteredIndexers.map((indexer) => (
                        <SelectIndexerRowConnector
                          key={`${indexer.implementation}-${indexer.name}`}
                          implementation={indexer.implementation}
                          {...indexer}
                          onIndexerSelect={onIndexerSelect}
                        />
                      ))
                    }
                  </TableBody>
                </Table> :
                null
            }
            {
              isPopulated && !!indexers.length && !filteredIndexers.length ?
                <Alert
                  kind={kinds.WARNING}
                >
                  {translate('NoIndexersFound')}
                </Alert> :
                null
            }
          </Scroller>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <div className={styles.available}>
            {
              isPopulated ?
                translate('CountIndexersAvailable', { count: filteredIndexers.length }) :
                null
            }
          </div>

          <div>
            <Button onPress={onModalClose}>{translate('Close')}</Button>
          </div>
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
