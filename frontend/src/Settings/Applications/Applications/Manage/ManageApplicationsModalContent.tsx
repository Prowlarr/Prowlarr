import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { ApplicationAppState } from 'App/State/SettingsAppState';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ConfirmModal from 'Components/Modal/ConfirmModal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import useSelectState from 'Helpers/Hooks/useSelectState';
import { kinds } from 'Helpers/Props';
import {
  bulkDeleteApplications,
  bulkEditApplications,
} from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import { SelectStateInputProps } from 'typings/props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import ManageApplicationsEditModal from './Edit/ManageApplicationsEditModal';
import ManageApplicationsModalRow from './ManageApplicationsModalRow';
import TagsModal from './Tags/TagsModal';
import styles from './ManageApplicationsModalContent.css';

// TODO: This feels janky to do, but not sure of a better way currently
type OnSelectedChangeCallback = React.ComponentProps<
  typeof ManageApplicationsModalRow
>['onSelectedChange'];

const COLUMNS = [
  {
    name: 'name',
    label: translate('Name'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'implementation',
    label: translate('Implementation'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'syncLevel',
    label: translate('SyncLevel'),
    isSortable: true,
    isVisible: true,
  },
  {
    name: 'tags',
    label: translate('Tags'),
    isSortable: true,
    isVisible: true,
  },
];

interface ManageApplicationsModalContentProps {
  onModalClose(): void;
}

function ManageApplicationsModalContent(
  props: ManageApplicationsModalContentProps
) {
  const { onModalClose } = props;

  const {
    isFetching,
    isPopulated,
    isDeleting,
    isSaving,
    error,
    items,
  }: ApplicationAppState = useSelector(
    createClientSideCollectionSelector('settings.applications')
  );
  const dispatch = useDispatch();

  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isTagsModalOpen, setIsTagsModalOpen] = useState(false);
  const [isSavingTags, setIsSavingTags] = useState(false);

  const [selectState, setSelectState] = useSelectState();

  const { allSelected, allUnselected, selectedState } = selectState;

  const selectedIds: number[] = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const selectedCount = selectedIds.length;

  const onDeletePress = useCallback(() => {
    setIsDeleteModalOpen(true);
  }, [setIsDeleteModalOpen]);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, [setIsDeleteModalOpen]);

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, [setIsEditModalOpen]);

  const onEditModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, [setIsEditModalOpen]);

  const onConfirmDelete = useCallback(() => {
    dispatch(bulkDeleteApplications({ ids: selectedIds }));
    setIsDeleteModalOpen(false);
  }, [selectedIds, dispatch]);

  const onSavePress = useCallback(
    (payload: object) => {
      setIsEditModalOpen(false);

      dispatch(
        bulkEditApplications({
          ids: selectedIds,
          ...payload,
        })
      );
    },
    [selectedIds, dispatch]
  );

  const onTagsPress = useCallback(() => {
    setIsTagsModalOpen(true);
  }, [setIsTagsModalOpen]);

  const onTagsModalClose = useCallback(() => {
    setIsTagsModalOpen(false);
  }, [setIsTagsModalOpen]);

  const onApplyTagsPress = useCallback(
    (tags: number[], applyTags: string) => {
      setIsSavingTags(true);
      setIsTagsModalOpen(false);

      dispatch(
        bulkEditApplications({
          ids: selectedIds,
          tags,
          applyTags,
        })
      );
    },
    [selectedIds, dispatch]
  );

  const onSelectAllChange = useCallback(
    ({ value }: SelectStateInputProps) => {
      setSelectState({ type: value ? 'selectAll' : 'unselectAll', items });
    },
    [items, setSelectState]
  );

  const onSelectedChange = useCallback<OnSelectedChangeCallback>(
    ({ id, value, shiftKey = false }) => {
      setSelectState({
        type: 'toggleSelected',
        items,
        id,
        isSelected: value,
        shiftKey,
      });
    },
    [items, setSelectState]
  );

  const errorMessage = getErrorMessage(
    error,
    'Unable to load download clients.'
  );
  const anySelected = selectedCount > 0;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('ManageApplications')}</ModalHeader>
      <ModalBody>
        {isFetching ? <LoadingIndicator /> : null}

        {error ? <div>{errorMessage}</div> : null}

        {isPopulated && !error && !items.length && (
          <Alert kind={kinds.INFO}>{translate('NoApplicationsFound')}</Alert>
        )}

        {isPopulated && !!items.length && !isFetching && !isFetching ? (
          <Table
            columns={COLUMNS}
            horizontalScroll={true}
            selectAll={true}
            allSelected={allSelected}
            allUnselected={allUnselected}
            onSelectAllChange={onSelectAllChange}
          >
            <TableBody>
              {items.map((item) => {
                return (
                  <ManageApplicationsModalRow
                    key={item.id}
                    isSelected={selectedState[item.id]}
                    {...item}
                    columns={COLUMNS}
                    onSelectedChange={onSelectedChange}
                  />
                );
              })}
            </TableBody>
          </Table>
        ) : null}
      </ModalBody>

      <ModalFooter>
        <div className={styles.leftButtons}>
          <SpinnerButton
            kind={kinds.DANGER}
            isSpinning={isDeleting}
            isDisabled={!anySelected}
            onPress={onDeletePress}
          >
            {translate('Delete')}
          </SpinnerButton>

          <SpinnerButton
            isSpinning={isSaving}
            isDisabled={!anySelected}
            onPress={onEditPress}
          >
            {translate('Edit')}
          </SpinnerButton>

          <SpinnerButton
            isSpinning={isSaving && isSavingTags}
            isDisabled={!anySelected}
            onPress={onTagsPress}
          >
            {translate('SetTags')}
          </SpinnerButton>
        </div>

        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>

      <ManageApplicationsEditModal
        isOpen={isEditModalOpen}
        onModalClose={onEditModalClose}
        onSavePress={onSavePress}
        applicationIds={selectedIds}
      />

      <TagsModal
        isOpen={isTagsModalOpen}
        ids={selectedIds}
        onApplyTagsPress={onApplyTagsPress}
        onModalClose={onTagsModalClose}
      />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        kind={kinds.DANGER}
        title={translate('DeleteSelectedApplications')}
        message={translate('DeleteSelectedApplicationsMessageText', [
          selectedIds.length,
        ])}
        confirmLabel={translate('Delete')}
        onConfirm={onConfirmDelete}
        onCancel={onDeleteModalClose}
      />
    </ModalContent>
  );
}

export default ManageApplicationsModalContent;
