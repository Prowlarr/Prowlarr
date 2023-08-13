import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import Indexer from 'Indexer/Indexer';
import { deleteIndexer } from 'Store/Actions/indexerActions';
import { createIndexerSelectorForHook } from 'Store/Selectors/createIndexerSelector';
import translate from 'Utilities/String/translate';

interface DeleteIndexerModalContentProps {
  indexerId: number;
  onModalClose(): void;
}

function DeleteIndexerModalContent(props: DeleteIndexerModalContentProps) {
  const { indexerId, onModalClose } = props;

  const { name } = useSelector(
    createIndexerSelectorForHook(indexerId)
  ) as Indexer;
  const dispatch = useDispatch();

  const onConfirmDelete = useCallback(() => {
    dispatch(deleteIndexer({ id: indexerId }));

    onModalClose();
  }, [indexerId, dispatch, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('Delete')} - {name}
      </ModalHeader>

      <ModalBody>
        {translate('AreYouSureYouWantToDeleteIndexer', { name })}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>

        <Button kind={kinds.DANGER} onPress={onConfirmDelete}>
          {translate('Delete')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteIndexerModalContent;
