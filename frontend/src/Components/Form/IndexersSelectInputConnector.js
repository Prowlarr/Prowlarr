import { groupBy, map } from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import titleCase from 'Utilities/String/titleCase';
import EnhancedSelectInput from './EnhancedSelectInput';

function createMapStateToProps() {
  return createSelector(
    (state, { value }) => value,
    (state) => state.indexers,
    (value, indexers) => {
      const values = [];
      const groupedIndexers = map(groupBy(indexers.items, 'protocol'), (val, key) => ({ protocol: key, indexers: val }));

      groupedIndexers.forEach((element) => {
        values.push({
          key: element.protocol === 'usenet' ? -1 : -2,
          value: titleCase(element.protocol)
        });

        if (element.indexers && element.indexers.length > 0) {
          element.indexers.forEach((indexer) => {
            values.push({
              key: indexer.id,
              value: indexer.name,
              hint: `(${indexer.id})`,
              isDisabled: !indexer.enable,
              parentKey: element.protocol === 'usenet' ? -1 : -2
            });
          });
        }
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
  value: PropTypes.arrayOf(PropTypes.oneOfType([PropTypes.string, PropTypes.number])).isRequired,
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  onChange: PropTypes.func.isRequired
};

export default connect(createMapStateToProps)(IndexersSelectInputConnector);
