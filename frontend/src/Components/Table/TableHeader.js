import PropTypes from 'prop-types';
import React from 'react';

function TableHeader({ children }) {

  //
  // Render

  return (
    <thead>
      <tr>
        {children}
      </tr>
    </thead>
  );
}

TableHeader.propTypes = {
  children: PropTypes.node
};

export default TableHeader;
