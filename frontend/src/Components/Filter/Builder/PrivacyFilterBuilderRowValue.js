import React from 'react';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';

const privacyTypes = [
  { id: 'public', name: translate('Public') },
  { id: 'private', name: translate('Private') },
  { id: 'semiPrivate', name: translate('SemiPrivate') }
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
