import { uniqBy } from 'lodash';
import React from 'react';
import Label from 'Components/Label';
import { IndexerCapabilities } from 'Indexer/Indexer';
import translate from 'Utilities/String/translate';

interface CapabilitiesLabelProps {
  capabilities: IndexerCapabilities;
  categoryFilter?: number[];
}

function CapabilitiesLabel(props: CapabilitiesLabelProps) {
  const { categoryFilter = [] } = props;

  const { categories = [] } = props.capabilities || ({} as IndexerCapabilities);

  let filteredList = categories.filter((item) => item.id < 100000);

  if (categoryFilter.length > 0) {
    filteredList = filteredList.filter(
      (item) =>
        categoryFilter.includes(item.id) ||
        (item.subCategories &&
          item.subCategories.some((r) => categoryFilter.includes(r.id)))
    );
  }

  const indexerCategories = uniqBy(filteredList, 'id').sort(
    (a, b) => a.id - b.id
  );

  return (
    <span>
      {indexerCategories.map((category) => {
        return (
          <Label key={category.id} title={`${category.id}`}>
            {category.name}
          </Label>
        );
      })}

      {filteredList.length === 0 ? <Label>{translate('None')}</Label> : null}
    </span>
  );
}

export default CapabilitiesLabel;
