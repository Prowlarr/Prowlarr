import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { inputTypes, kinds } from 'Helpers/Props';
import SettingsToolbarConnector from 'Settings/SettingsToolbarConnector';
import translate from 'Utilities/String/translate';

const logLevelOptions = [
  { key: 'info', value: 'Info' },
  { key: 'debug', value: 'Debug' },
  { key: 'trace', value: 'Trace' }
];

class DevelopmentSettings extends Component {

  //
  // Render

  render() {
    const {
      isFetching,
      error,
      settings,
      hasSettings,
      onInputChange,
      onSavePress,
      ...otherProps
    } = this.props;

    return (
      <PageContent title={translate('DevelopmentSettings')}>
        <SettingsToolbarConnector
          {...otherProps}
          onSavePress={onSavePress}
        />

        <PageContentBody>
          {
            isFetching &&
              <LoadingIndicator />
          }

          {
            !isFetching && error &&
              <Alert kind={kinds.DANGER}>
                {translate('UnableToLoadDevelopmentSettings')}
              </Alert>
          }

          {
            hasSettings && !isFetching && !error &&
              <Form
                id="developmentSettings"
                {...otherProps}
              >
                <FieldSet legend={translate('Logging')}>
                  <FormGroup>
                    <FormLabel>{translate('SettingsLogRotate')}</FormLabel>

                    <FormInputGroup
                      type={inputTypes.NUMBER}
                      name="logRotate"
                      helpText={translate('SettingsLogRotateHelpText')}
                      onChange={onInputChange}
                      {...settings.logRotate}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>{translate('SettingsConsoleLogLevel')}</FormLabel>
                    <FormInputGroup
                      type={inputTypes.SELECT}
                      name="consoleLogLevel"
                      values={logLevelOptions}
                      onChange={onInputChange}
                      {...settings.consoleLogLevel}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>{translate('SettingsLogSql')}</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="logSql"
                      helpText={translate('SettingsSqlLoggingHelpText')}
                      onChange={onInputChange}
                      {...settings.logSql}
                    />
                  </FormGroup>

                  <FormGroup>
                    <FormLabel>{translate('SettingsIndexerLogging')}</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="logIndexerResponse"
                      helpText={translate('SettingsIndexerLoggingHelpText')}
                      onChange={onInputChange}
                      {...settings.logIndexerResponse}
                    />
                  </FormGroup>
                </FieldSet>

                <FieldSet legend={translate('Analytics')}>
                  <FormGroup>
                    <FormLabel>{translate('SettingsFilterSentryEvents')}</FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="filterSentryEvents"
                      helpText={translate('SettingsFilterSentryEventsHelpText')}
                      onChange={onInputChange}
                      {...settings.filterSentryEvents}
                    />
                  </FormGroup>
                </FieldSet>
              </Form>
          }
        </PageContentBody>
      </PageContent>
    );
  }

}

DevelopmentSettings.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  error: PropTypes.object,
  settings: PropTypes.object.isRequired,
  hasSettings: PropTypes.bool.isRequired,
  onSavePress: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default DevelopmentSettings;
