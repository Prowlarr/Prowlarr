import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IndexersSelectInputConnector from 'Components/Form/IndexersSelectInputConnector';
import NewznabCategorySelectInputConnector from 'Components/Form/NewznabCategorySelectInputConnector';
import TextInput from 'Components/Form/TextInput';
import keyboardShortcuts from 'Components/keyboardShortcuts';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import translate from 'Utilities/String/translate';
import SearchFooterLabel from './SearchFooterLabel';
import styles from './SearchFooter.css';

class SearchFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    const {
      defaultIndexerIds,
      defaultCategories,
      defaultSearchQuery
    } = props;

    this.state = {
      searchingReleases: false,
      searchQuery: defaultSearchQuery,
      searchIndexerIds: defaultIndexerIds,
      searchCategories: defaultCategories
    };
  }

  componentDidMount() {
    const {
      searchIndexerIds,
      searchCategories,
      searchQuery
    } = this.state;

    if (searchQuery !== '' || searchCategories.length > 0 || searchIndexerIds.length > 0) {
      this.onSearchPress();
    }

    this.props.bindShortcut('enter', this.onSearchPress, { isGlobal: true });
  }

  componentDidUpdate(prevProps) {
    const {
      isFetching,
      defaultIndexerIds,
      defaultCategories,
      defaultSearchQuery,
      searchError
    } = this.props;

    const {
      searchIndexerIds,
      searchCategories,
      searchQuery
    } = this.state;

    const newState = {};

    if (searchIndexerIds !== defaultIndexerIds) {
      newState.searchIndexerIds = defaultIndexerIds;
    }

    if (searchCategories !== defaultCategories) {
      newState.searchCategories = defaultCategories;
    }

    if (searchQuery !== defaultSearchQuery) {
      newState.searchQuery = defaultSearchQuery;
    }

    if (prevProps.isFetching && !isFetching && !searchError) {
      newState.searchingReleases = false;
    }

    if (!_.isEmpty(newState)) {
      this.setState(newState);
    }
  }

  //
  // Listeners

  onSearchPress = () => {
    this.props.onSearchPress(this.state.searchQuery, this.state.searchIndexerIds, this.state.searchCategories);
  }

  //
  // Render

  render() {
    const {
      isFetching,
      hasIndexers,
      onInputChange
    } = this.props;

    const {
      searchQuery,
      searchIndexerIds,
      searchCategories
    } = this.state;

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <SearchFooterLabel
            label={'Query'}
            isSaving={false}
          />

          <TextInput
            name='searchQuery'
            autoFocus={true}
            value={searchQuery}
            isDisabled={isFetching}
            onChange={onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={'Indexers'}
            isSaving={false}
          />

          <IndexersSelectInputConnector
            name='searchIndexerIds'
            value={searchIndexerIds}
            isDisabled={isFetching}
            onChange={onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={'Categories'}
            isSaving={false}
          />

          <NewznabCategorySelectInputConnector
            name='searchCategories'
            value={searchCategories}
            isDisabled={isFetching}
            onChange={onInputChange}
          />
        </div>

        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <SearchFooterLabel
              label={`Search ${searchIndexerIds.length === 0 ? 'all' : searchIndexerIds.length} Indexers`}
              isSaving={false}
            />

            <div className={styles.buttons}>

              <SpinnerButton
                className={styles.searchButton}
                isSpinning={isFetching}
                isDisabled={isFetching || !hasIndexers}
                onPress={this.onSearchPress}
              >
                {translate('Search')}
              </SpinnerButton>
            </div>
          </div>
        </div>
      </PageContentFooter>
    );
  }
}

SearchFooter.propTypes = {
  defaultIndexerIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  defaultCategories: PropTypes.arrayOf(PropTypes.number).isRequired,
  defaultSearchQuery: PropTypes.string.isRequired,
  isFetching: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  searchError: PropTypes.object,
  bindShortcut: PropTypes.func.isRequired
};

export default keyboardShortcuts(SearchFooter);
