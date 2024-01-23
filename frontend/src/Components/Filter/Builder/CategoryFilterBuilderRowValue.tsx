import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { IndexerCategory } from 'Indexer/Indexer';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

const indexerCategoriesSelector = createSelector(
  (state: AppState) => state.settings.indexerCategories,
  (categories) => categories.items
);

function CategoryFilterBuilderRowValue(props: FilterBuilderRowValueProps) {
  const categories: IndexerCategory[] = useSelector(indexerCategoriesSelector);

  const tagList = categories.reduce(
    (acc: { id: number; name: string }[], element) => {
      acc.push({
        id: element.id,
        name: `${element.name} (${element.id})`,
      });

      if (element.subCategories && element.subCategories.length > 0) {
        element.subCategories.forEach((subCat) => {
          acc.push({
            id: subCat.id,
            name: `${subCat.name} (${subCat.id})`,
          });
        });
      }

      return acc;
    },
    []
  );

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default CategoryFilterBuilderRowValue;
