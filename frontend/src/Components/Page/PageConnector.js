import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import { saveDimensions, setIsSidebarVisible } from 'Store/Actions/appActions';
import { fetchCustomFilters } from 'Store/Actions/customFilterActions';
import { fetchIndexers } from 'Store/Actions/indexerActions';
import { fetchIndexerStatus } from 'Store/Actions/indexerStatusActions';
import { fetchAppProfiles, fetchGeneralSettings, fetchIndexerCategories, fetchLanguages, fetchUISettings } from 'Store/Actions/settingsActions';
import { fetchStatus } from 'Store/Actions/systemActions';
import { fetchTags } from 'Store/Actions/tagActions';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import ErrorPage from './ErrorPage';
import LoadingPage from './LoadingPage';
import Page from './Page';

function testLocalStorage() {
  const key = 'prowlarrTest';

  try {
    localStorage.setItem(key, key);
    localStorage.removeItem(key);

    return true;
  } catch (e) {
    return false;
  }
}

const selectAppProps = createSelector(
  (state) => state.app.isSidebarVisible,
  (state) => state.app.version,
  (state) => state.app.isUpdated,
  (state) => state.app.isDisconnected,
  (isSidebarVisible, version, isUpdated, isDisconnected) => {
    return {
      isSidebarVisible,
      version,
      isUpdated,
      isDisconnected
    };
  }
);

const selectIsPopulated = createSelector(
  (state) => state.customFilters.isPopulated,
  (state) => state.tags.isPopulated,
  (state) => state.settings.ui.isPopulated,
  (state) => state.settings.general.isPopulated,
  (state) => state.settings.languages.isPopulated,
  (state) => state.settings.appProfiles.isPopulated,
  (state) => state.indexers.isPopulated,
  (state) => state.indexerStatus.isPopulated,
  (state) => state.settings.indexerCategories.isPopulated,
  (state) => state.system.status.isPopulated,
  (
    customFiltersIsPopulated,
    tagsIsPopulated,
    uiSettingsIsPopulated,
    generalSettingsIsPopulated,
    languagesIsPopulated,
    appProfilesIsPopulated,
    indexersIsPopulated,
    indexerStatusIsPopulated,
    indexerCategoriesIsPopulated,
    systemStatusIsPopulated
  ) => {
    return (
      customFiltersIsPopulated &&
      tagsIsPopulated &&
      uiSettingsIsPopulated &&
      generalSettingsIsPopulated &&
      languagesIsPopulated &&
      appProfilesIsPopulated &&
      indexersIsPopulated &&
      indexerStatusIsPopulated &&
      indexerCategoriesIsPopulated &&
      systemStatusIsPopulated
    );
  }
);

const selectErrors = createSelector(
  (state) => state.customFilters.error,
  (state) => state.tags.error,
  (state) => state.settings.ui.error,
  (state) => state.settings.general.error,
  (state) => state.settings.languages.error,
  (state) => state.settings.appProfiles.error,
  (state) => state.indexers.error,
  (state) => state.indexerStatus.error,
  (state) => state.settings.indexerCategories.error,
  (state) => state.system.status.error,
  (
    customFiltersError,
    tagsError,
    uiSettingsError,
    generalSettingsError,
    languagesError,
    appProfilesError,
    indexersError,
    indexerStatusError,
    indexerCategoriesError,
    systemStatusError
  ) => {
    const hasError = !!(
      customFiltersError ||
      tagsError ||
      uiSettingsError ||
      generalSettingsError ||
      languagesError ||
      appProfilesError ||
      indexersError ||
      indexerStatusError ||
      indexerCategoriesError ||
      systemStatusError
    );

    return {
      hasError,
      customFiltersError,
      tagsError,
      uiSettingsError,
      generalSettingsError,
      languagesError,
      appProfilesError,
      indexersError,
      indexerStatusError,
      indexerCategoriesError,
      systemStatusError
    };
  }
);

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.ui.item.enableColorImpairedMode,
    selectIsPopulated,
    selectErrors,
    selectAppProps,
    createDimensionsSelector(),
    (
      enableColorImpairedMode,
      isPopulated,
      errors,
      app,
      dimensions
    ) => {
      return {
        ...app,
        ...errors,
        isPopulated,
        isSmallScreen: dimensions.isSmallScreen,
        enableColorImpairedMode
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    dispatchFetchCustomFilters() {
      dispatch(fetchCustomFilters());
    },
    dispatchFetchTags() {
      dispatch(fetchTags());
    },
    dispatchFetchLanguages() {
      dispatch(fetchLanguages());
    },
    dispatchFetchIndexers() {
      dispatch(fetchIndexers());
    },
    dispatchFetchIndexerStatus() {
      dispatch(fetchIndexerStatus());
    },
    dispatchFetchIndexerCategories() {
      dispatch(fetchIndexerCategories());
    },
    dispatchFetchUISettings() {
      dispatch(fetchUISettings());
    },
    dispatchFetchAppProfiles() {
      dispatch(fetchAppProfiles());
    },
    dispatchFetchGeneralSettings() {
      dispatch(fetchGeneralSettings());
    },
    dispatchFetchStatus() {
      dispatch(fetchStatus());
    },
    onResize(dimensions) {
      dispatch(saveDimensions(dimensions));
    },
    onSidebarVisibleChange(isSidebarVisible) {
      dispatch(setIsSidebarVisible({ isSidebarVisible }));
    }
  };
}

class PageConnector extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isLocalStorageSupported: testLocalStorage()
    };
  }

  componentDidMount() {
    if (!this.props.isPopulated) {
      this.props.dispatchFetchCustomFilters();
      this.props.dispatchFetchTags();
      this.props.dispatchFetchLanguages();
      this.props.dispatchFetchAppProfiles();
      this.props.dispatchFetchIndexers();
      this.props.dispatchFetchIndexerStatus();
      this.props.dispatchFetchIndexerCategories();
      this.props.dispatchFetchUISettings();
      this.props.dispatchFetchGeneralSettings();
      this.props.dispatchFetchStatus();
    }
  }

  //
  // Listeners

  onSidebarToggle = () => {
    this.props.onSidebarVisibleChange(!this.props.isSidebarVisible);
  }

  //
  // Render

  render() {
    const {
      isPopulated,
      hasError,
      dispatchFetchTags,
      dispatchFetchLanguages,
      dispatchFetchAppProfiles,
      dispatchFetchIndexers,
      dispatchFetchIndexerStatus,
      dispatchFetchIndexerCategories,
      dispatchFetchUISettings,
      dispatchFetchGeneralSettings,
      dispatchFetchStatus,
      ...otherProps
    } = this.props;

    if (hasError || !this.state.isLocalStorageSupported) {
      return (
        <ErrorPage
          {...this.state}
          {...otherProps}
        />
      );
    }

    if (isPopulated) {
      return (
        <Page
          {...otherProps}
          onSidebarToggle={this.onSidebarToggle}
        />
      );
    }

    return (
      <LoadingPage />
    );
  }
}

PageConnector.propTypes = {
  isPopulated: PropTypes.bool.isRequired,
  hasError: PropTypes.bool.isRequired,
  isSidebarVisible: PropTypes.bool.isRequired,
  dispatchFetchCustomFilters: PropTypes.func.isRequired,
  dispatchFetchTags: PropTypes.func.isRequired,
  dispatchFetchLanguages: PropTypes.func.isRequired,
  dispatchFetchAppProfiles: PropTypes.func.isRequired,
  dispatchFetchIndexers: PropTypes.func.isRequired,
  dispatchFetchIndexerStatus: PropTypes.func.isRequired,
  dispatchFetchIndexerCategories: PropTypes.func.isRequired,
  dispatchFetchUISettings: PropTypes.func.isRequired,
  dispatchFetchGeneralSettings: PropTypes.func.isRequired,
  dispatchFetchStatus: PropTypes.func.isRequired,
  onSidebarVisibleChange: PropTypes.func.isRequired
};

export default withRouter(
  connect(createMapStateToProps, createMapDispatchToProps)(PageConnector)
);
