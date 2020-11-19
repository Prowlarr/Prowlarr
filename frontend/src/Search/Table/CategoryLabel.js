import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CategoryLabel({ categories }) {
  let catName = '';

  if (categories && categories.length > 0) {
    catName = categories[0].name;
  }

  return (
    <Label>
      {catName}
    </Label>
  );
}

CategoryLabel.propTypes = {
  categories: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default CategoryLabel;
