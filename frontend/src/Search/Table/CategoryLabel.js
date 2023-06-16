import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import Tooltip from 'Components/Tooltip/Tooltip';
import { kinds, tooltipPositions } from 'Helpers/Props';

function CategoryLabel({ categories }) {
  const sortedCategories = categories.filter((cat) => cat.name !== undefined).sort((c) => c.id);

  if (categories?.length === 0) {
    return (
      <Tooltip
        anchor={<Label kind={kinds.DANGER}>Unknown</Label>}
        tooltip="Please report this issue to the GitHub as this shouldn't be happening"
        position={tooltipPositions.LEFT}
      />
    );
  }

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

CategoryLabel.defaultProps = {
  categories: []
};

CategoryLabel.propTypes = {
  categories: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default CategoryLabel;
