import PropTypes from 'prop-types';
import React, { useEffect, useRef } from 'react';
import Alert from 'Components/Alert';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import { authenticationMethodOptions, authenticationRequiredOptions } from 'Settings/General/SecuritySettings';
import translate from 'Utilities/String/translate';
import styles from './AuthenticationRequiredModalContent.css';

function onModalClose() {
  // No-op
}

function AuthenticationRequiredModalContent(props) {
  const {
    isPopulated,
    error,
    isSaving,
    saveError,
    settings,
    onInputChange,
    onSavePress,
    dispatchFetchStatus
  } = props;

  const {
    authenticationMethod,
    authenticationRequired,
    username,
    password,
    passwordConfirmation,
    allowedHosts
  } = settings;

  const authenticationEnabled = authenticationMethod && authenticationMethod.value !== 'none';

  const didMount = useRef(false);

  useEffect(() => {
    if (!isSaving && !saveError && didMount.current) {
      dispatchFetchStatus();
    }

    didMount.current = true;
  }, [isSaving, saveError, dispatchFetchStatus]);

  return (
    <ModalContent
      showCloseButton={false}
      onModalClose={onModalClose}
    >
      <ModalHeader>
        {translate('AuthenticationRequired')}
      </ModalHeader>

      <ModalBody>
        <Alert
          className={styles.authRequiredAlert}
          kind={kinds.WARNING}
        >
          {translate('AuthenticationRequiredWarning')}
        </Alert>

        {
          isPopulated && !error ?
            <div>
              <FormGroup>
                <FormLabel>{translate('AuthenticationMethod')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="authenticationMethod"
                  values={authenticationMethodOptions}
                  helpText={translate('AuthenticationMethodHelpText')}
                  helpTextWarning={authenticationMethod.value === 'none' ? translate('AuthenticationMethodHelpTextWarning') : undefined}
                  helpLink="https://wiki.servarr.com/prowlarr/faq#forced-authentication"
                  onChange={onInputChange}
                  {...authenticationMethod}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('AuthenticationRequired')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.SELECT}
                  name="authenticationRequired"
                  values={authenticationRequiredOptions}
                  helpText={translate('AuthenticationRequiredHelpText')}
                  onChange={onInputChange}
                  {...authenticationRequired}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('Username')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.TEXT}
                  name="username"
                  onChange={onInputChange}
                  helpTextWarning={username?.value ? undefined : translate('AuthenticationRequiredUsernameHelpTextWarning')}
                  {...username}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('Password')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.PASSWORD}
                  name="password"
                  onChange={onInputChange}
                  helpTextWarning={password?.value ? undefined : translate('AuthenticationRequiredPasswordHelpTextWarning')}
                  {...password}
                />
              </FormGroup>

              <FormGroup>
                <FormLabel>{translate('PasswordConfirmation')}</FormLabel>

                <FormInputGroup
                  type={inputTypes.PASSWORD}
                  name="passwordConfirmation"
                  onChange={onInputChange}
                  helpTextWarning={passwordConfirmation?.value ? undefined : translate('AuthenticationRequiredPasswordConfirmationHelpTextWarning')}
                  {...passwordConfirmation}
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
            </div> :
            null
        }

        {
          !isPopulated && !error ? <LoadingIndicator /> : null
        }
      </ModalBody>

      <ModalFooter>
        <SpinnerErrorButton
          kind={kinds.PRIMARY}
          isSpinning={isSaving}
          isDisabled={!authenticationEnabled}
          error={saveError}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

AuthenticationRequiredModalContent.propTypes = {
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  settings: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onSavePress: PropTypes.func.isRequired,
  dispatchFetchStatus: PropTypes.func.isRequired
};

export default AuthenticationRequiredModalContent;
