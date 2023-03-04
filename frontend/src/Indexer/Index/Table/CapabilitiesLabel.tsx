import React from 'react';
import Label from 'Components/Label';
import { IndexerCapabilities } from 'Indexer/Indexer';

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

  const nameList = Array.from(
    new Set(filteredList.map((item) => item.name).sort())
  );

  return (
    <span>
      {nameList.map((category) => {
        return <Label key={category}>{category}</Label>;
      })}

      {filteredList.length === 0 ? <Label>{'None'}</Label> : null}
    </span>
  );
}

export default CapabilitiesLabel;
