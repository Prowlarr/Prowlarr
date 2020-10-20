import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CapabilitiesLabel(props) {
  const {
    supportsBooks,
    supportsMovies,
    supportsMusic,
    supportsTv
  } = props;

  return (
    <span>
      {
        supportsBooks ?
          <Label>
            {'Books'}
          </Label> :
          null
      }

      {
        supportsMovies ?
          <Label>
            {'Movies'}
          </Label> :
          null
      }

      {
        supportsMusic ?
          <Label>
            {'Music'}
          </Label> :
          null
      }

      {
        supportsTv ?
          <Label>
            {'TV'}
          </Label> :
          null
      }

      {
        !supportsTv && !supportsMusic && !supportsMovies && !supportsBooks ?
          <Label>
            {'None'}
          </Label> :
          null
      }
    </span>
  );
}

CapabilitiesLabel.propTypes = {
  supportsTv: PropTypes.bool.isRequired,
  supportsBooks: PropTypes.bool.isRequired,
  supportsMusic: PropTypes.bool.isRequired,
  supportsMovies: PropTypes.bool.isRequired
};

export default CapabilitiesLabel;
