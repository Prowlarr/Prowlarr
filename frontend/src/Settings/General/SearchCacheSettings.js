import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

function SearchCacheSettings(props) {
  const {
    settings,
    onInputChange
  } = props;

  const {
    searchCacheEnabled,
    searchCacheTtl
  } = settings;

  return (
    <FieldSet legend={translate('SearchCache')}>
      <FormGroup size={sizes.MEDIUM}>
        <FormLabel>{translate('SearchCacheEnabled')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="searchCacheEnabled"
          helpText={translate('SearchCacheEnabledHelpText')}
          onChange={onInputChange}
          {...searchCacheEnabled}
        />
      </FormGroup>

      {
        searchCacheEnabled.value &&
          <FormGroup>
            <FormLabel>{translate('SearchCacheTtl')}</FormLabel>

            <FormInputGroup
              type={inputTypes.NUMBER}
              name="searchCacheTtl"
              min={1}
              unit="minutes"
              helpText={translate('SearchCacheTtlHelpText')}
              onChange={onInputChange}
              {...searchCacheTtl}
            />
          </FormGroup>
      }
    </FieldSet>
  );
}

SearchCacheSettings.propTypes = {
  settings: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default SearchCacheSettings;
