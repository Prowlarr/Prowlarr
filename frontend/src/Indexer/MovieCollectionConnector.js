import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import createMovieCollectionListSelector from 'Store/Selectors/createMovieCollectionListSelector';
import MovieCollection from './MovieCollection';

function createMapStateToProps() {
  return createSelector(
    createIndexerSelector(),
    createMovieCollectionListSelector(),
    (movie, collectionList) => {
      const {
        monitored,
        qualityProfileId,
        minimumAvailability
      } = movie;

      return {
        collectionList,
        monitored,
        qualityProfileId,
        minimumAvailability
      };
    }
  );
}

class MovieCollectionConnector extends Component {

  //
  // Listeners

  onMonitorTogglePress = (monitored) => {

  }

  //
  // Render

  render() {
    return (
      <MovieCollection
        {...this.props}
        onMonitorTogglePress={this.onMonitorTogglePress}
      />
    );
  }
}

MovieCollectionConnector.propTypes = {
  tmdbId: PropTypes.number.isRequired,
  movieId: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired,
  collectionList: PropTypes.object,
  monitored: PropTypes.bool.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  minimumAvailability: PropTypes.string.isRequired,
  isSaving: PropTypes.bool.isRequired
};

export default connect(createMapStateToProps)(MovieCollectionConnector);
