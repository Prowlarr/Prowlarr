import PropTypes from 'prop-types';
import React from 'react';

function TableHeader({ children, secondaryHeaderRow }) {
  return (
    <thead>
      <tr>
        {children}
      </tr>
      {secondaryHeaderRow}
    </thead>
  );
}

TableHeader.propTypes = {
  children: PropTypes.node,
  secondaryHeaderRow: PropTypes.node
};

export default TableHeader;
