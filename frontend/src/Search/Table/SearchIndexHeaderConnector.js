import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { setReleasesTableOption } from 'Store/Actions/releaseActions';
import SearchIndexHeader from './SearchIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setReleasesTableOption(payload));
    }
  };
}

class SearchIndexHeaderConnector extends Component {
  //
  // Lifecycle
  // Added so parent knows that is rendered
  // So it knows what the width of the `title` column is
  componentDidMount() {
    this.props.setTitleReady(true);
  }

  render() {
    const { setTitleReady, ...otherProps } = this.props;
    return (
      <SearchIndexHeader {...otherProps} />
    );
  }
}

SearchIndexHeaderConnector.propTypes = {
  setTitleReady: PropTypes.func.isRequired
};

export default connect(undefined, createMapDispatchToProps)(SearchIndexHeaderConnector);
