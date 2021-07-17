import PropTypes from 'prop-types';
import React, { useEffect, useRef, useState } from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import IndexersSelectInputConnector from 'Components/Form/IndexersSelectInputConnector';
import NewznabCategorySelectInputConnector from 'Components/Form/NewznabCategorySelectInputConnector';
import TextInput from 'Components/Form/TextInput';
import keyboardShortcuts from 'Components/keyboardShortcuts';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import SearchFooterLabel from './SearchFooterLabel';
import styles from './SearchFooter.css';

function createMapStateToProps() {
  return createSelector(
    (state) => state.releases,
    (releases) => {
      const {
        searchQuery: defaultSearchQuery,
        searchIndexerIds: defaultIndexerIds,
        searchCategories: defaultCategories
      } = releases.defaults;

      return {
        defaultSearchQuery,
        defaultIndexerIds,
        defaultCategories
      };
    }
  );
}

function SearchFooter({
  bindShortcut, searchError,
  isFetching, hasIndexers, onSearchPress: onSearchPressProps
}) {
  const [, setSearchingReleases] = useState(false);
  const { defaultSearchQuery, defaultCategories, defaultIndexerIds } = useSelector(createMapStateToProps());
  const [searchQuery, setSearchQuery] = useState(defaultSearchQuery);
  const [searchIndexerIds, setSearchIndexerIds] = useState(defaultIndexerIds);
  const [searchCategories, setSearchCategories] = useState(defaultCategories);
  const ref = useRef({ searchQuery, searchIndexerIds, searchCategories });

  //
  // Listeners

  function onSearchPress() {
    const { searchQuery: refSQ, searchIndexerIds: refSII, searchCategories: refSC } = ref.current;
    onSearchPressProps(refSQ, refSII, refSC);
  }

  function onTextChange({ value }) {
    setSearchQuery(value);
    ref.current.searchQuery = value;
  }

  function onCategoryChange({ value }) {
    setSearchCategories(value);
    ref.current.searchCategories = value;
  }

  function onIndexerChange({ value }) {
    setSearchIndexerIds(value);
    ref.current.searchIndexerIds = value;
  }

  //
  // State handlers

  useEffect(() => {
    if (searchQuery !== '' || searchCategories.length > 0 || searchIndexerIds.length > 0) {
      onSearchPress();
    }

    bindShortcut('enter', onSearchPress, { isGlobal: true });
  }, []);

  useEffect(() => {
    if (searchIndexerIds !== defaultIndexerIds) {
      setSearchIndexerIds(defaultIndexerIds);
    }
  }, [defaultIndexerIds]);

  useEffect(() => {
    if (searchCategories !== defaultCategories) {
      setSearchCategories(defaultCategories);
    }
  }, [defaultCategories]);

  useEffect(() => {
    if (!isFetching && !searchError) {
      setSearchingReleases(false);
    }
  }, [isFetching]);

  //
  // Render

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
          onChange={onTextChange}
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
          onChange={onIndexerChange}
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
          onChange={onCategoryChange}
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
              onPress={onSearchPress}
            >
              Search
            </SpinnerButton>
          </div>
        </div>
      </div>
    </PageContentFooter>
  );
}

SearchFooter.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired,
  searchError: PropTypes.object,
  bindShortcut: PropTypes.func.isRequired
};

export default keyboardShortcuts(SearchFooter);
