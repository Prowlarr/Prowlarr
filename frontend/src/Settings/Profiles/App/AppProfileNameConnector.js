import PropTypes from 'prop-types';
import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createAppProfileSelector from 'Store/Selectors/createAppProfileSelector';

function createMapStateToProps() {
  return createSelector(
    createAppProfileSelector(),
    (appProfile) => {
      return {
        name: appProfile.name
      };
    }
  );
}

function AppProfileNameConnector({ name, ...otherProps }) {
  return (
    <span>
      {name}
    </span>
  );
}

AppProfileNameConnector.propTypes = {
  appProfileId: PropTypes.number.isRequired,
  name: PropTypes.string.isRequired
};

export default connect(createMapStateToProps)(AppProfileNameConnector);
