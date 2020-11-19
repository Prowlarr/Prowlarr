import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CapabilitiesLabel(props) {
  const {
    categories
  } = props.capabilities;

  const filteredList = categories.filter((item) => item.id < 100000).map((item) => item.name).sort();

  return (
    <span>
      {
        filteredList.map((category) => {
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
  capabilities: PropTypes.object.isRequired
};

CapabilitiesLabel.defaultProps = {
  capabilities: {
    categories: []
  }
};

export default CapabilitiesLabel;
