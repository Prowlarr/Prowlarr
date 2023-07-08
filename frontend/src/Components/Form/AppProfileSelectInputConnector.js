import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByName from 'Utilities/Array/sortByName';
import translate from 'Utilities/String/translate';
import SelectInput from './SelectInput';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.appProfiles', sortByName),
    (state, { includeNoChange }) => includeNoChange,
    (state, { includeMixed }) => includeMixed,
    (appProfiles, includeNoChange, includeMixed) => {
      const values = _.map(appProfiles.items, (appProfile) => {
        return {
          key: appProfile.id,
          value: appProfile.name
        };
      });

      if (includeNoChange) {
        values.unshift({
          key: 'noChange',
          value: translate('NoChange'),
          disabled: true
        });
      }

      if (includeMixed) {
        values.unshift({
          key: 'mixed',
          value: '(Mixed)',
          disabled: true
        });
      }

      return {
        values
      };
    }
  );
}

class AppProfileSelectInputConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      name,
      value,
      values
    } = this.props;

    if (!value || !values.some((v) => v.key === value) ) {
      const firstValue = _.find(values, (option) => !isNaN(parseInt(option.key)));

      if (firstValue) {
        this.onChange({ name, value: firstValue.key });
      }
    }
  }

  //
  // Listeners

  onChange = ({ name, value }) => {
    this.props.onChange({ name, value: parseInt(value) });
  };

  //
  // Render

  render() {
    return (
      <SelectInput
        {...this.props}
        onChange={this.onChange}
      />
    );
  }
}

AppProfileSelectInputConnector.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.oneOfType([PropTypes.number, PropTypes.string]),
  values: PropTypes.arrayOf(PropTypes.object).isRequired,
  includeNoChange: PropTypes.bool.isRequired,
  onChange: PropTypes.func.isRequired
};

AppProfileSelectInputConnector.defaultProps = {
  includeNoChange: false
};

export default connect(createMapStateToProps)(AppProfileSelectInputConnector);
