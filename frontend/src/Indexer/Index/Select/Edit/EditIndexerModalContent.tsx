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
import translate from 'Utilities/String/translate';
import styles from './EditIndexerModalContent.css';

interface SavePayload {
  enable?: boolean;
  appProfileId?: number;
}

interface EditIndexerModalContentProps {
  indexerIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

const NO_CHANGE = 'noChange';

const enableOptions = [
  { key: NO_CHANGE, value: translate('NoChange'), disabled: true },
  { key: 'true', value: translate('Enabled') },
  { key: 'false', value: translate('Disabled') },
];

function EditIndexerModalContent(props: EditIndexerModalContentProps) {
  const { indexerIds, onSavePress, onModalClose } = props;

  const [enable, setEnable] = useState(NO_CHANGE);
  const [appProfileId, setAppProfileId] = useState<string | number>(NO_CHANGE);

  const save = useCallback(() => {
    let hasChanges = false;
    const payload: SavePayload = {};

    if (enable !== NO_CHANGE) {
      hasChanges = true;
      payload.enable = enable === 'true';
    }

    if (appProfileId !== NO_CHANGE) {
      hasChanges = true;
      payload.appProfileId = appProfileId as number;
    }

    if (hasChanges) {
      onSavePress(payload);
    }

    onModalClose();
  }, [enable, appProfileId, onSavePress, onModalClose]);

  const onInputChange = useCallback(
    ({ name, value }) => {
      switch (name) {
        case 'enable':
          setEnable(value);
          break;
        case 'appProfileId':
          setAppProfileId(value);
          break;
        default:
          console.warn(`EditIndexersModalContent Unknown Input: '${name}'`);
      }
    },
    [setEnable]
  );

  const onSavePressWrapper = useCallback(() => {
    save();
  }, [save]);

  const selectedCount = indexerIds.length;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('EditSelectedIndexers')}</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>{translate('Enable')}</FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="enable"
            value={enable}
            values={enableOptions}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup>
          <FormLabel>{translate('SyncProfile')}</FormLabel>

          <FormInputGroup
            type={inputTypes.APP_PROFILE_SELECT}
            name="appProfileId"
            value={appProfileId}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            onChange={onInputChange}
          />
        </FormGroup>
      </ModalBody>

      <ModalFooter className={styles.modalFooter}>
        <div className={styles.selected}>
          {translate('CountIndexersSelected', [selectedCount])}
        </div>

        <div>
          <Button onPress={onModalClose}>{translate('Cancel')}</Button>

          <Button onPress={onSavePressWrapper}>
            {translate('ApplyChanges')}
          </Button>
        </div>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditIndexerModalContent;
