import React, { useCallback, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import { ApplicationSyncLevel } from 'typings/Application';
import translate from 'Utilities/String/translate';
import styles from './ManageApplicationsEditModalContent.css';

interface SavePayload {
  syncLevel?: ApplicationSyncLevel;
}

interface ManageApplicationsEditModalContentProps {
  applicationIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

const NO_CHANGE = 'noChange';

const syncLevelOptions = [
  {
    key: NO_CHANGE,
    get value() {
      return translate('NoChange');
    },
    isDisabled: true,
  },
  {
    key: ApplicationSyncLevel.Disabled,
    get value() {
      return translate('Disabled');
    },
  },
  {
    key: ApplicationSyncLevel.AddOnly,
    get value() {
      return translate('AddRemoveOnly');
    },
  },
  {
    key: ApplicationSyncLevel.FullSync,
    get value() {
      return translate('FullSync');
    },
  },
];

function ManageApplicationsEditModalContent(
  props: ManageApplicationsEditModalContentProps
) {
  const { applicationIds, onSavePress, onModalClose } = props;

  const [syncLevel, setSyncLevel] = useState(NO_CHANGE);

  const save = useCallback(() => {
    let hasChanges = false;
    const payload: SavePayload = {};

    if (syncLevel !== NO_CHANGE) {
      hasChanges = true;
      payload.syncLevel = syncLevel as ApplicationSyncLevel;
    }

    if (hasChanges) {
      onSavePress(payload);
    }

    onModalClose();
  }, [syncLevel, onSavePress, onModalClose]);

  const onInputChange = useCallback(
    ({ name, value }: { name: string; value: string }) => {
      switch (name) {
        case 'syncLevel':
          setSyncLevel(value);
          break;
        default:
          console.warn(`EditApplicationsModalContent Unknown Input: '${name}'`);
      }
    },
    []
  );

  const selectedCount = applicationIds.length;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('EditSelectedApplications')}</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>{translate('SyncLevel')}</FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="syncLevel"
            value={syncLevel}
            values={syncLevelOptions}
            helpTexts={[
              translate('SyncLevelAddRemove'),
              translate('SyncLevelFull'),
            ]}
            onChange={onInputChange}
          />
        </FormGroup>
      </ModalBody>

      <ModalFooter className={styles.modalFooter}>
        <div className={styles.selected}>
          {translate('CountApplicationsSelected', { count: selectedCount })}
        </div>

        <div>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <Button onPress={save}>{translate('ApplyChanges')}</Button>
        </div>
      </ModalFooter>
    </ModalContent>
  );
}

export default ManageApplicationsEditModalContent;
