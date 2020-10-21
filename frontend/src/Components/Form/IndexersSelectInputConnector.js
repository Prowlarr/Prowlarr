import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { value }) => value,
    (state) => state.indexers,
    (value, indexers) => {
      const values = indexers.items.map(({ id, name }) => {
        return {
          key: id,
          value: name
        };
      });

      return {
        value,
        values
      };
    }
  );
}

class IndexersSelectInputConnector extends Component {

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

IndexersSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  indexerIds: PropTypes.number,
  value: PropTypes.arrayOf(PropTypes.number).isRequired,
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  onChange: PropTypes.func.isRequired
};

export default connect(createMapStateToProps)(IndexersSelectInputConnector);
