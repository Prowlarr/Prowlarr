import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { value }) => value,
    (state) => state.settings.indexerCategories,
    (value, categories) => {
      const values = [];

      categories.items.forEach((element) => {
        values.push({
          key: element.id,
          value: element.name,
          hint: `(${element.id})`
        });

        if (element.subCategories && element.subCategories.length > 0) {
          element.subCategories.forEach((subCat) => {
            values.push({
              key: subCat.id,
              value: subCat.name,
              hint: `(${subCat.id})`,
              parentKey: element.id
            });
          });
        }
      });

      return {
        value: value || [],
        values
      };
    }
  );
}

class IndexersSelectInputConnector extends Component {

  onChange = ({ name, value }) => {
    this.props.onChange({ name, value });
  };

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
