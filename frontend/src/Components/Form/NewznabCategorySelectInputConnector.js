import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { value }) => value,
    (state) => state.settings.indexerCategories,
    (state) => state.indexers.items,
    (state, { indexerIds }) => indexerIds,
    (value, categories, indexers, indexerIds) => {
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

      if (indexerIds && indexerIds.length === 1) {
        const indexer = indexers.find((i) => i.id === indexerIds[0]);
        const indexerCategories = (indexer?.capabilities?.categories ?? [])
          .filter((category) => category.id >= 100000);

        if (indexerCategories.length) {
          const customCategoryGroup = {
            key: `indexerCategories-${indexer.id}`,
            value: indexer.name,
            isDisabled: true
          };

          values.push(customCategoryGroup);

          indexerCategories.forEach((category) => {
            values.push({
              key: category.id,
              value: category.name,
              hint: `(${category.id})`,
              parentKey: customCategoryGroup.key
            });
          });
        }
      }

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
  indexerIds: PropTypes.arrayOf(PropTypes.number),
  value: PropTypes.arrayOf(PropTypes.number).isRequired,
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  onChange: PropTypes.func.isRequired
};

export default connect(createMapStateToProps)(IndexersSelectInputConnector);
