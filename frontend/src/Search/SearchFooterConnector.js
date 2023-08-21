import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSearchDefault } from 'Store/Actions/releaseActions';
import parseUrl from 'Utilities/String/parseUrl';
import SearchFooter from './SearchFooter';

function createMapStateToProps() {
  return createSelector(
    (state) => state.releases,
    (state) => state.router.location,
    (releases, location) => {
      const {
        searchQuery: defaultSearchQuery,
        searchIndexerIds: defaultIndexerIds,
        searchCategories: defaultCategories,
        searchType: defaultSearchType,
        searchLimit: defaultSearchLimit,
        searchOffset: defaultSearchOffset
      } = releases.defaults;

      const { params } = parseUrl(location.search);
      const defaultSearchQueryParams = {};

      if (params.query && !defaultSearchQuery) {
        defaultSearchQueryParams.searchQuery = params.query;
      }

      if (params.indexerIds && !defaultIndexerIds.length) {
        defaultSearchQueryParams.searchIndexerIds = params.indexerIds.split(',').map((id) => Number(id)).filter(Boolean);
      }

      if (params.categories && !defaultCategories.length) {
        defaultSearchQueryParams.searchCategories = params.categories.split(',').map((id) => Number(id)).filter(Boolean);
      }

      if (params.type && defaultSearchType === 'search') {
        defaultSearchQueryParams.searchType = params.type;
      }

      if (params.limit && defaultSearchLimit === 100 && !isNaN(params.limit)) {
        defaultSearchQueryParams.searchLimit = Number(params.limit);
      }

      if (params.offset && !defaultSearchOffset && !isNaN(params.offset)) {
        defaultSearchQueryParams.searchOffset = Number(params.offset);
      }

      return {
        defaultSearchQueryParams,
        defaultSearchQuery,
        defaultIndexerIds,
        defaultCategories,
        defaultSearchType,
        defaultSearchLimit,
        defaultSearchOffset
      };
    }
  );
}

const mapDispatchToProps = {
  setSearchDefault
};

class SearchFooterConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    // Set defaults from query parameters
    Object.entries(this.props.defaultSearchQueryParams).forEach(([name, value]) => {
      this.onInputChange({ name, value });
    });
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.props.setSearchDefault({ [name]: value });
  };

  //
  // Render

  render() {
    return (
      <SearchFooter
        {...this.props}
        onInputChange={this.onInputChange}
      />
    );
  }
}

SearchFooterConnector.propTypes = {
  defaultSearchQueryParams: PropTypes.object.isRequired,
  setSearchDefault: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SearchFooterConnector);
