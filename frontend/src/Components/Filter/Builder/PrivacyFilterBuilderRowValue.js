import React from 'react';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const privacyTypes = [
  {
    id: 'public',
    get name() {
      return translate('Public');
    }
  },
  {
    id: 'private',
    get name() {
      return translate('Private');
    }
  },
  {
    id: 'semiPrivate',
    get name() {
      return translate('SemiPrivate');
    }
  }
];

function PrivacyFilterBuilderRowValue(props) {
  return (
    <FilterBuilderRowValue
      tagList={privacyTypes}
      {...props}
    />
  );
}

export default PrivacyFilterBuilderRowValue;
