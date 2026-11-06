import React from 'react';
import FieldSet from 'Components/FieldSet';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import useShowAdvancedSettings from 'Helpers/Hooks/useShowAdvancedSettings';
import { inputTypes, sizes } from 'Helpers/Props';
import useSystemStatus from 'System/useSystemStatus';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import General from 'typings/Settings/General';
import translate from 'Utilities/String/translate';

interface HostSettingsProps {
  bindAddress: PendingSection<General>['bindAddress'];
  port: PendingSection<General>['port'];
  urlBase: PendingSection<General>['urlBase'];
  instanceName: PendingSection<General>['instanceName'];
  applicationUrl: PendingSection<General>['applicationUrl'];
  allowedHosts: PendingSection<General>['allowedHosts'];
  enableSsl: PendingSection<General>['enableSsl'];
  sslPort: PendingSection<General>['sslPort'];
  sslCertPath: PendingSection<General>['sslCertPath'];
  sslCertPassword: PendingSection<General>['sslCertPassword'];
  launchBrowser: PendingSection<General>['launchBrowser'];
  onInputChange: (change: InputChanged) => void;
}

function HostSettings({
  bindAddress,
  port,
  urlBase,
  instanceName,
  applicationUrl,
  allowedHosts,
  enableSsl,
  sslPort,
  sslCertPath,
  sslCertPassword,
  launchBrowser,
  onInputChange,
}: HostSettingsProps) {
  const showAdvancedSettings = useShowAdvancedSettings();
  const { isWindows, mode } = useSystemStatus();

  return (
    <FieldSet legend={translate('Host')}>
      <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
        <FormLabel>{translate('BindAddress')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="bindAddress"
          helpText={translate('BindAddressHelpText')}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          onChange={onInputChange}
          {...bindAddress}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('PortNumber')}</FormLabel>

        <FormInputGroup
          type={inputTypes.NUMBER}
          name="port"
          min={1}
          max={65535}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          onChange={onInputChange}
          {...port}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('URLBase')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="urlBase"
          helpText={translate('UrlBaseHelpText')}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          onChange={onInputChange}
          {...urlBase}
        />
      </FormGroup>

      <FormGroup>
        <FormLabel>{translate('AllowedHosts')}</FormLabel>
        <FormInputGroup
          type={inputTypes.TEXT}
          name="allowedHosts"
          helpText={translate('AllowedHostsHelpText')}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          helpLink="https://wiki.servarr.com/prowlarr/settings#host"
          onChange={onInputChange}
          {...allowedHosts}
        />
      </FormGroup>

      <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
        <FormLabel>{translate('InstanceName')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="instanceName"
          helpText={translate('InstanceNameHelpText')}
          helpTextWarning={translate('RestartRequiredHelpTextWarning')}
          onChange={onInputChange}
          {...instanceName}
        />
      </FormGroup>

      <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
        <FormLabel>{translate('ApplicationURL')}</FormLabel>

        <FormInputGroup
          type={inputTypes.TEXT}
          name="applicationUrl"
          helpText={translate('ApplicationUrlHelpText')}
          onChange={onInputChange}
          {...applicationUrl}
        />
      </FormGroup>

      <FormGroup
        advancedSettings={showAdvancedSettings}
        isAdvanced={true}
        size={sizes.MEDIUM}
      >
        <FormLabel>{translate('EnableSSL')}</FormLabel>

        <FormInputGroup
          type={inputTypes.CHECK}
          name="enableSsl"
          helpText={translate('EnableSslHelpText')}
          onChange={onInputChange}
          {...enableSsl}
        />
      </FormGroup>

      {enableSsl.value ? (
        <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
          <FormLabel>{translate('SSLPort')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="sslPort"
            min={1}
            max={65535}
            helpTextWarning={translate('RestartRequiredHelpTextWarning')}
            onChange={onInputChange}
            {...sslPort}
          />
        </FormGroup>
      ) : null}

      {enableSsl.value ? (
        <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
          <FormLabel>{translate('SSLCertPath')}</FormLabel>

          <FormInputGroup
            type={inputTypes.TEXT}
            name="sslCertPath"
            helpText={translate('SSLCertPathHelpText')}
            helpTextWarning={translate('RestartRequiredHelpTextWarning')}
            onChange={onInputChange}
            {...sslCertPath}
          />
        </FormGroup>
      ) : null}

      {enableSsl.value ? (
        <FormGroup advancedSettings={showAdvancedSettings} isAdvanced={true}>
          <FormLabel>{translate('SSLCertPassword')}</FormLabel>

          <FormInputGroup
            type={inputTypes.PASSWORD}
            name="sslCertPassword"
            helpText={translate('SSLCertPasswordHelpText')}
            helpTextWarning={translate('RestartRequiredHelpTextWarning')}
            onChange={onInputChange}
            {...sslCertPassword}
          />
        </FormGroup>
      ) : null}

      {isWindows && mode !== 'service' ? (
        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('OpenBrowserOnStart')}</FormLabel>

          <FormInputGroup
            type={inputTypes.CHECK}
            name="launchBrowser"
            helpText={translate('LaunchBrowserHelpText')}
            onChange={onInputChange}
            {...launchBrowser}
          />
        </FormGroup>
      ) : null}
    </FieldSet>
  );
}

export default HostSettings;
