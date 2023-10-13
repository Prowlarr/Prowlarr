import React from 'react';
import Label from 'Components/Label';
import Tooltip from 'Components/Tooltip/Tooltip';
import { kinds, tooltipPositions } from 'Helpers/Props';
import { IndexerCategory } from 'Indexer/Indexer';
import translate from 'Utilities/String/translate';

interface CategoryLabelProps {
  categories: IndexerCategory[];
}

function CategoryLabel({ categories = [] }: CategoryLabelProps) {
  if (categories?.length === 0) {
    return (
      <Tooltip
        anchor={<Label kind={kinds.DANGER}>{translate('Unknown')}</Label>}
        tooltip="Please report this issue to the GitHub as this shouldn't be happening"
        position={tooltipPositions.LEFT}
      />
    );
  }

  const sortedCategories = categories
    .filter((cat) => cat.name !== undefined)
    .sort((a, b) => a.id - b.id);

  return (
    <span>
      {sortedCategories.map((category) => {
        return <Label key={category.id}>{category.name}</Label>;
      })}
    </span>
  );
}

export default CategoryLabel;
