import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { useSelect } from 'App/SelectContext';
import AppState from 'App/State/AppState';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import usePrevious from 'Helpers/Hooks/usePrevious';
import { kinds } from 'Helpers/Props';
import { bulkEditIndexers } from 'Store/Actions/indexerActions';
import translate from 'Utilities/String/translate';
import getSelectedIds from 'Utilities/Table/getSelectedIds';
import DeleteIndexerModal from './Delete/DeleteIndexerModal';
import EditIndexerModal from './Edit/EditIndexerModal';
import TagsModal from './Tags/TagsModal';
import styles from './IndexerIndexSelectFooter.css';

interface SavePayload {
  enable?: boolean;
  appProfileId?: number;
  priority?: number;
  minimumSeeders?: number;
  seedRatio?: number;
  seedTime?: number;
  packSeedTime?: number;
}

const indexersEditorSelector = createSelector(
  (state: AppState) => state.indexers,
  (indexers) => {
    const { isSaving, isDeleting, deleteError } = indexers;

    return {
      isSaving,
      isDeleting,
      deleteError,
    };
  }
);

function IndexerIndexSelectFooter() {
  const { isSaving, isDeleting, deleteError } = useSelector(
    indexersEditorSelector
  );

  const dispatch = useDispatch();

  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isTagsModalOpen, setIsTagsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isSavingIndexer, setIsSavingIndexer] = useState(false);
  const [isSavingTags, setIsSavingTags] = useState(false);
  const previousIsDeleting = usePrevious(isDeleting);

  const [selectState, selectDispatch] = useSelect();
  const { selectedState } = selectState;

  const indexerIds = useMemo(() => {
    return getSelectedIds(selectedState);
  }, [selectedState]);

  const selectedCount = indexerIds.length;

  const onEditPress = useCallback(() => {
    setIsEditModalOpen(true);
  }, [setIsEditModalOpen]);

  const onEditModalClose = useCallback(() => {
    setIsEditModalOpen(false);
  }, [setIsEditModalOpen]);

  const onSavePress = useCallback(
    (payload: SavePayload) => {
      setIsSavingIndexer(true);
      setIsEditModalOpen(false);

      dispatch(
        bulkEditIndexers({
          ...payload,
          ids: indexerIds,
        })
      );
    },
    [indexerIds, dispatch]
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
        bulkEditIndexers({
          ids: indexerIds,
          tags,
          applyTags,
        })
      );
    },
    [indexerIds, dispatch]
  );

  const onDeletePress = useCallback(() => {
    setIsDeleteModalOpen(true);
  }, [setIsDeleteModalOpen]);

  const onDeleteModalClose = useCallback(() => {
    setIsDeleteModalOpen(false);
  }, []);

  useEffect(() => {
    if (!isSaving) {
      setIsSavingIndexer(false);
      setIsSavingTags(false);
    }
  }, [isSaving]);

  useEffect(() => {
    if (previousIsDeleting && !isDeleting && !deleteError) {
      selectDispatch({ type: 'unselectAll' });
    }
  }, [previousIsDeleting, isDeleting, deleteError, selectDispatch]);

  const anySelected = selectedCount > 0;

  return (
    <PageContentFooter className={styles.footer}>
      <div className={styles.buttons}>
        <div className={styles.actionButtons}>
          <SpinnerButton
            isSpinning={isSaving && isSavingIndexer}
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

        <div className={styles.deleteButtons}>
          <SpinnerButton
            kind={kinds.DANGER}
            isSpinning={isDeleting}
            isDisabled={!anySelected || isDeleting}
            onPress={onDeletePress}
          >
            {translate('Delete')}
          </SpinnerButton>
        </div>
      </div>

      <div className={styles.selected}>
        {translate('CountIndexersSelected', { count: selectedCount })}
      </div>

      <EditIndexerModal
        isOpen={isEditModalOpen}
        indexerIds={indexerIds}
        onSavePress={onSavePress}
        onModalClose={onEditModalClose}
      />

      <TagsModal
        isOpen={isTagsModalOpen}
        indexerIds={indexerIds}
        onApplyTagsPress={onApplyTagsPress}
        onModalClose={onTagsModalClose}
      />

      <DeleteIndexerModal
        isOpen={isDeleteModalOpen}
        indexerIds={indexerIds}
        onModalClose={onDeleteModalClose}
      />
    </PageContentFooter>
  );
}

export default IndexerIndexSelectFooter;
