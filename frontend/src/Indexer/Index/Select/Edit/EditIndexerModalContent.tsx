import React, { useCallback, useState } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './EditIndexerModalContent.css';

interface SavePayload {
  enable?: boolean;
  appProfileId?: number;
  priority?: number;
  minimumSeeders?: number;
  seedRatio?: number;
  seedTime?: number;
  packSeedTime?: number;
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
  const [priority, setPriority] = useState<null | string | number>(null);
  const [minimumSeeders, setMinimumSeeders] = useState<null | string | number>(
    null
  );
  const [seedRatio, setSeedRatio] = useState<null | string | number>(null);
  const [seedTime, setSeedTime] = useState<null | string | number>(null);
  const [packSeedTime, setPackSeedTime] = useState<null | string | number>(
    null
  );

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

    if (priority !== null) {
      hasChanges = true;
      payload.priority = priority as number;
    }

    if (minimumSeeders !== null) {
      hasChanges = true;
      payload.minimumSeeders = minimumSeeders as number;
    }

    if (seedRatio !== null) {
      hasChanges = true;
      payload.seedRatio = seedRatio as number;
    }

    if (seedTime !== null) {
      hasChanges = true;
      payload.seedTime = seedTime as number;
    }

    if (packSeedTime !== null) {
      hasChanges = true;
      payload.packSeedTime = packSeedTime as number;
    }

    if (hasChanges) {
      onSavePress(payload);
    }

    onModalClose();
  }, [
    enable,
    appProfileId,
    priority,
    minimumSeeders,
    seedRatio,
    seedTime,
    packSeedTime,
    onSavePress,
    onModalClose,
  ]);

  const onInputChange = useCallback(
    ({ name, value }) => {
      switch (name) {
        case 'enable':
          setEnable(value);
          break;
        case 'appProfileId':
          setAppProfileId(value);
          break;
        case 'priority':
          setPriority(value);
          break;
        case 'minimumSeeders':
          setMinimumSeeders(value);
          break;
        case 'seedRatio':
          setSeedRatio(value);
          break;
        case 'seedTime':
          setSeedTime(value);
          break;
        case 'packSeedTime':
          setPackSeedTime(value);
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
        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('Enable')}</FormLabel>

          <FormInputGroup
            type={inputTypes.SELECT}
            name="enable"
            value={enable}
            values={enableOptions}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('SyncProfile')}</FormLabel>

          <FormInputGroup
            type={inputTypes.APP_PROFILE_SELECT}
            name="appProfileId"
            value={appProfileId}
            helpText={translate('AppProfileSelectHelpText')}
            includeNoChange={true}
            includeNoChangeDisabled={false}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('IndexerPriority')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="priority"
            value={priority}
            min={1}
            max={50}
            helpText={translate('IndexerPriorityHelpText')}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('AppsMinimumSeeders')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="minimumSeeders"
            value={minimumSeeders}
            helpText={translate('AppsMinimumSeedersHelpText')}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('SeedRatio')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="seedRatio"
            value={seedRatio}
            helpText={translate('SeedRatioHelpText')}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('SeedTime')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="seedTime"
            value={seedTime}
            unit={translate('minutes')}
            helpText={translate('SeedTimeHelpText')}
            onChange={onInputChange}
          />
        </FormGroup>

        <FormGroup size={sizes.MEDIUM}>
          <FormLabel>{translate('PackSeedTime')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="packSeedTime"
            value={packSeedTime}
            unit={translate('minutes')}
            helpText={translate('PackSeedTimeHelpText')}
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
