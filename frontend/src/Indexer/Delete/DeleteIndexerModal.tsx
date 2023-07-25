import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import DeleteIndexerModalContent from './DeleteIndexerModalContent';

interface DeleteIndexerModalProps {
  isOpen: boolean;
  indexerId: number;
  onModalClose(): void;
}

function DeleteIndexerModal(props: DeleteIndexerModalProps) {
  const { isOpen, indexerId, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} size={sizes.MEDIUM} onModalClose={onModalClose}>
      <DeleteIndexerModalContent
        indexerId={indexerId}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default DeleteIndexerModal;
