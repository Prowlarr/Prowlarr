import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CapabilitiesLabel(props) {
  const {
    movieSearchAvailable,
    tvSearchAvailable,
    musicSearchAvailable,
    bookSearchAvailable
  } = props.capabilities;

  return (
    <span>
      {
        bookSearchAvailable ?
          <Label>
            {'Books'}
          </Label> :
          null
      }

      {
        movieSearchAvailable ?
          <Label>
            {'Movies'}
          </Label> :
          null
      }

      {
        musicSearchAvailable ?
          <Label>
            {'Music'}
          </Label> :
          null
      }

      {
        tvSearchAvailable ?
          <Label>
            {'TV'}
          </Label> :
          null
      }

      {
        !tvSearchAvailable && !musicSearchAvailable && !movieSearchAvailable && !bookSearchAvailable ?
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

export default CapabilitiesLabel;
