import { isEmpty } from 'lodash-es';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FormInputButton from 'Components/Form/FormInputButton';
import FormInputGroup from 'Components/Form/FormInputGroup';
import IndexersSelectInputConnector from 'Components/Form/IndexersSelectInputConnector';
import NewznabCategorySelectInputConnector from 'Components/Form/NewznabCategorySelectInputConnector';
import Icon from 'Components/Icon';
import keyboardShortcuts from 'Components/keyboardShortcuts';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import QueryParameterModal from './QueryParameterModal';
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
      defaultSearchQuery,
      defaultSearchType
    } = props;

    this.state = {
      isQueryParameterModalOpen: false,
      queryModalOptions: null,
      searchType: defaultSearchType,
      searchingReleases: false,
      searchQuery: defaultSearchQuery || '',
      searchIndexerIds: defaultIndexerIds,
      searchCategories: defaultCategories,
      searchLimit: 100,
      searchOffset: 0,
      newSearch: true
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
      defaultSearchType,
      searchError
    } = this.props;

    const {
      searchIndexerIds,
      searchCategories,
      searchType
    } = this.state;

    const newState = {};

    if (defaultSearchQuery && defaultSearchQuery !== prevProps.defaultSearchQuery) {
      newState.searchQuery = defaultSearchQuery;
      newState.searchOffset = 0;
      newState.newSearch = true;
    }

    if (searchType !== defaultSearchType) {
      newState.searchType = defaultSearchType;
    }

    if (searchIndexerIds !== defaultIndexerIds) {
      newState.searchIndexerIds = defaultIndexerIds;
    }

    if (searchCategories !== defaultCategories) {
      newState.searchCategories = defaultCategories;
    }

    if (prevProps.isFetching && !isFetching && !searchError) {
      newState.searchingReleases = false;
    }

    if (!isEmpty(newState)) {
      this.setState(newState);
    }
  }

  //
  // Listeners

  onQueryParameterModalOpenClick = () => {
    this.setState({
      queryModalOptions: {
        name: 'queryParameters'
      },
      isQueryParameterModalOpen: true
    });
  };

  onQueryParameterModalClose = () => {
    this.setState({ isQueryParameterModalOpen: false });
  };

  onSearchPress = () => {

    const {
      searchLimit,
      searchOffset,
      searchQuery,
      searchIndexerIds,
      searchCategories,
      searchType
    } = this.state;

    this.props.onSearchPress(searchQuery, searchIndexerIds, searchCategories, searchType, searchLimit, searchOffset);

    this.setState({ searchOffset: searchOffset + 100, newSearch: false });
  };

  onSearchInputChange = ({ value }) => {
    this.setState({ searchQuery: value, newSearch: true, searchOffset: 0 });
  };

  onInputChange = ({ name, value }) => {
    this.props.onInputChange({ name, value });
    this.setState({ newSearch: true, searchOffset: 0 });
  };

  //
  // Render

  render() {
    const {
      isFetching,
      isPopulated,
      isGrabbing,
      hasIndexers,
      onInputChange,
      onBulkGrabPress,
      itemCount,
      selectedCount
    } = this.props;

    const {
      searchQuery,
      searchIndexerIds,
      searchCategories,
      newSearch,
      isQueryParameterModalOpen,
      queryModalOptions,
      searchType
    } = this.state;

    let icon = icons.SEARCH;

    switch (searchType) {
      case 'book':
        icon = icons.BOOK;
        break;
      case 'tvsearch':
        icon = icons.TV;
        break;
      case 'movie':
        icon = icons.FILM;
        break;
      case 'music':
        icon = icons.AUDIO;
        break;
      default:
        icon = icons.SEARCH;
    }

    let footerLabel = `Search ${searchIndexerIds.length === 0 ? 'all' : searchIndexerIds.length} Indexers`;

    if (isPopulated) {
      footerLabel = selectedCount === 0 ? `Found ${itemCount} releases` : `Selected ${selectedCount} of ${itemCount} releases`;
    }

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <SearchFooterLabel
            label={translate('Query')}
            isSaving={false}
          />

          <FormInputGroup
            type={inputTypes.TEXT}
            name="searchQuery"
            value={searchQuery}
            buttons={
              <FormInputButton onPress={this.onQueryParameterModalOpenClick}>
                <Icon
                  name={icon}
                />
              </FormInputButton>}
            onChange={this.onSearchInputChange}
            onFocus={this.onApikeyFocus}
            isDisabled={isFetching}
            {...searchQuery}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={translate('Indexers')}
            isSaving={false}
          />

          <IndexersSelectInputConnector
            name='searchIndexerIds'
            value={searchIndexerIds}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={translate('Categories')}
            isSaving={false}
          />

          <NewznabCategorySelectInputConnector
            name='searchCategories'
            value={searchCategories}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <SearchFooterLabel
              className={styles.selectedReleasesLabel}
              label={footerLabel}
              isSaving={false}
            />

            <div className={styles.buttons}>

              {
                isPopulated &&
                  <SpinnerButton
                    className={styles.searchButton}
                    kind={kinds.SUCCESS}
                    isSpinning={isGrabbing}
                    isDisabled={isFetching || !hasIndexers || selectedCount === 0}
                    onPress={onBulkGrabPress}
                  >
                    {translate('GrabReleases')}
                  </SpinnerButton>
              }

              <SpinnerButton
                className={styles.searchButton}
                isSpinning={isFetching}
                isDisabled={isFetching || !hasIndexers}
                onPress={this.onSearchPress}
              >
                {newSearch ? translate('Search') : translate('More')}
              </SpinnerButton>
            </div>
          </div>
        </div>

        <QueryParameterModal
          isOpen={isQueryParameterModalOpen}
          {...queryModalOptions}
          name='queryParameters'
          value={searchQuery}
          searchType={searchType}
          onSearchInputChange={this.onSearchInputChange}
          onInputChange={onInputChange}
          onModalClose={this.onQueryParameterModalClose}
        />
      </PageContentFooter>
    );
  }
}

SearchFooter.propTypes = {
  defaultIndexerIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  defaultCategories: PropTypes.arrayOf(PropTypes.number).isRequired,
  defaultSearchQuery: PropTypes.string.isRequired,
  defaultSearchType: PropTypes.string.isRequired,
  selectedCount: PropTypes.number.isRequired,
  itemCount: PropTypes.number.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  isGrabbing: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  onBulkGrabPress: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired,
  onInputChange: PropTypes.func.isRequired,
  searchError: PropTypes.object,
  bindShortcut: PropTypes.func.isRequired
};

export default keyboardShortcuts(SearchFooter);
