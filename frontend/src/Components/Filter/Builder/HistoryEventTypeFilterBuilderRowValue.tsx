import React from 'react';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

const EVENT_TYPE_OPTIONS = [
  {
    id: 1,
    get name() {
      return translate('Grabbed');
    },
  },
  {
    id: 3,
    get name() {
      return translate('IndexerRss');
    },
  },
  {
    id: 2,
    get name() {
      return translate('IndexerQuery');
    },
  },
  {
    id: 4,
    get name() {
      return translate('IndexerAuth');
    },
  },
];

function HistoryEventTypeFilterBuilderRowValue(
  props: FilterBuilderRowValueProps
) {
  return <FilterBuilderRowValue {...props} tagList={EVENT_TYPE_OPTIONS} />;
}

export default HistoryEventTypeFilterBuilderRowValue;
