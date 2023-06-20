import PropTypes from 'prop-types';
import React from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NotificationEventItems.css';

function NotificationEventItems(props) {
  const {
    item,
    onInputChange
  } = props;

  const {
    onGrab,
    onHealthIssue,
    onHealthRestored,
    onApplicationUpdate,
    supportsOnGrab,
    includeManualGrabs,
    supportsOnHealthIssue,
    supportsOnHealthRestored,
    includeHealthWarnings,
    supportsOnApplicationUpdate
  } = item;

  return (
    <FormGroup>
      <FormLabel>{translate('NotificationTriggers')}</FormLabel>
      <div>
        <FormInputHelpText
          text={translate('NotificationTriggersHelpText')}
          link="https://wiki.servarr.com/prowlarr/settings#connections"
        />
        <div className={styles.events}>
          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onGrab"
              helpText={translate('OnGrabHelpText')}
              isDisabled={!supportsOnGrab.value}
              {...onGrab}
              onChange={onInputChange}
            />
          </div>

          {
            onGrab.value &&
              <div>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="includeManualGrabs"
                  helpText={translate('IncludeManualGrabsHelpText')}
                  isDisabled={!supportsOnGrab.value}
                  {...includeManualGrabs}
                  onChange={onInputChange}
                />
              </div>
          }

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onHealthIssue"
              helpText={translate('OnHealthIssueHelpText')}
              isDisabled={!supportsOnHealthIssue.value}
              {...onHealthIssue}
              onChange={onInputChange}
            />
          </div>

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onHealthRestored"
              helpText={translate('OnHealthRestoredHelpText')}
              isDisabled={!supportsOnHealthRestored.value}
              {...onHealthRestored}
              onChange={onInputChange}
            />
          </div>

          {
            (onHealthIssue.value || onHealthRestored.value) &&
              <div>
                <FormInputGroup
                  type={inputTypes.CHECK}
                  name="includeHealthWarnings"
                  helpText={translate('IncludeHealthWarningsHelpText')}
                  isDisabled={!supportsOnHealthIssue.value}
                  {...includeHealthWarnings}
                  onChange={onInputChange}
                />
              </div>
          }

          <div>
            <FormInputGroup
              type={inputTypes.CHECK}
              name="onApplicationUpdate"
              helpText={translate('OnApplicationUpdateHelpText')}
              isDisabled={!supportsOnApplicationUpdate.value}
              {...onApplicationUpdate}
              onChange={onInputChange}
            />
          </div>
        </div>
      </div>
    </FormGroup>
  );
}

NotificationEventItems.propTypes = {
  item: PropTypes.object.isRequired,
  onInputChange: PropTypes.func.isRequired
};

export default NotificationEventItems;
