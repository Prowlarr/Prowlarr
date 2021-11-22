import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setSearchDefault } from 'Store/Actions/releaseActions';
import SearchFooter from './SearchFooter';

function createMapStateToProps() {
  return createSelector(
    (state) => state.releases,
    (releases) => {
      const {
        searchQuery: defaultSearchQuery,
        searchIndexerIds: defaultIndexerIds,
        searchCategories: defaultCategories,
        searchType: defaultSearchType
      } = releases.defaults;

      return {
        defaultSearchQuery,
        defaultIndexerIds,
        defaultCategories,
        defaultSearchType
      };
    }
  );
}

const mapDispatchToProps = {
  setSearchDefault
};

class SearchFooterConnector extends Component {

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
  setSearchDefault: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(SearchFooterConnector);
