import React from 'react';
import Modal from 'Components/Modal/Modal';
import EditIndexerModalContent from './EditIndexerModalContent';

interface EditIndexerModalProps {
  isOpen: boolean;
  indexerIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

function EditIndexerModal(props: EditIndexerModalProps) {
  const { isOpen, indexerIds, onSavePress, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <EditIndexerModalContent
        indexerIds={indexerIds}
        onSavePress={onSavePress}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default EditIndexerModal;
