import React from 'react';
import Modal from 'Components/Modal/Modal';
import DeleteIndexerModalContent from './DeleteIndexerModalContent';

interface DeleteIndexerModalProps {
  isOpen: boolean;
  indexerIds: number[];
  onModalClose(): void;
}

function DeleteIndexerModal(props: DeleteIndexerModalProps) {
  const { isOpen, indexerIds, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <DeleteIndexerModalContent
        indexerIds={indexerIds}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default DeleteIndexerModal;
