import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CapabilitiesLabel(props) {
  const {
    categoryFilter
  } = props;

  const {
    categories
  } = props.capabilities;

  let filteredList = categories.filter((item) => item.id < 100000);

  if (categoryFilter.length > 0) {
    filteredList = filteredList.filter((item) => categoryFilter.includes(item.id));
  }

  const nameList = filteredList.map((item) => item.name).sort();

  return (
    <span>
      {
        nameList.map((category) => {
          return (
            <Label key={category}>
              {category}
            </Label>
          );
        })
      }

      {
        filteredList.length === 0 ?
          <Label>
            {'None'}
          </Label> :
          null
      }
    </span>
  );
}

CapabilitiesLabel.propTypes = {
  capabilities: PropTypes.object.isRequired,
  categoryFilter: PropTypes.arrayOf(PropTypes.number).isRequired
};

CapabilitiesLabel.defaultProps = {
  capabilities: {
    categories: []
  },
  categoryFilter: []
};

export default CapabilitiesLabel;
