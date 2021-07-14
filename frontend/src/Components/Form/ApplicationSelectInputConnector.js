import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    (state) => state.settings.applications.items,
    (applications) => {
      const values = [];

      applications.forEach((application) => {
        values.push({
          key: application.id,
          value: application.name
        });
      });

      return {
        values
      };
    }
  );
}

class ApplicationSelectInputConnector extends Component {

  //
  // Listeners

  onChange = ({ name, value }) => {
    this.props.onChange({ name, value });
  }

  //
  // Render

  render() {
    return (
      <EnhancedSelectInput
        {...this.props}
        onChange={this.onChange}
      />
    );
  }
}

ApplicationSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  applicationIds: PropTypes.arrayOf(PropTypes.number),
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  onChange: PropTypes.func.isRequired
};

ApplicationSelectInputConnector.defaultProps = {
  includeNoChange: false
};

export default connect(createMapStateToProps)(ApplicationSelectInputConnector);
