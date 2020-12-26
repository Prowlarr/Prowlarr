import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';

function CategoryLabel({ categories }) {
  const sortedCategories = categories.sort((c) => c.id);

  return (
    <span>
      {
        sortedCategories.map((category) => {
          return (
            <Label key={category.name}>
              {category.name}
            </Label>
          );
        })
      }
    </span>
  );
}

CategoryLabel.propTypes = {
  categories: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default CategoryLabel;
