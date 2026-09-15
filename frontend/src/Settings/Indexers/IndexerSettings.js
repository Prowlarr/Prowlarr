import PropTypes from 'prop-types';
import React from 'react';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes } from 'Helpers/Props';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';
import IndexerProxiesConnector from './IndexerProxies/IndexerProxiesConnector';

function IndexerSettings(props) {
  const {
    isFetching,
    error,
    settings,
    hasSettings,
    defaultUserAgent,
    onInputChange,
    onSavePress,
    ...otherProps
  } = props;

  return (
    <PageContent title={translate('IndexerSettings')}>
      <SettingsToolbarConnector
        {...otherProps}
        onSavePress={onSavePress}
      />

      <PageContentBody>
        <IndexerProxiesConnector />

        {
          isFetching && !hasSettings &&
            <LoadingIndicator />
        }

        {
          !isFetching && !!error &&
            <div>
              {translate('UnableToLoadIndexerSettings')}
            </div>
        }

        {
          hasSettings && !isFetching && !error &&
            <Form>
              <FieldSet legend={translate('Options')}>
                <FormGroup>
                  <FormLabel>{translate('IndexerUserAgent')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="indexerUserAgent"
                    helpText={translate('IndexerGlobalUserAgentHelpText')}
                    placeholder={defaultUserAgent}
                    {...settings.indexerUserAgent}
                    onChange={onInputChange}
                  />
                </FormGroup>
              </FieldSet>
            </Form>
        }

      </PageContentBody>
    </PageContent>
  );
}

IndexerSettings.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  defaultUserAgent: PropTypes.string,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired
};

export default IndexerSettings;
