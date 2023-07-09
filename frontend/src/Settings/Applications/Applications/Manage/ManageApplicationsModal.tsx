import React from 'react';
import Modal from 'Components/Modal/Modal';
import ManageApplicationsModalContent from './ManageApplicationsModalContent';

interface ManageApplicationsModalProps {
  isOpen: boolean;
  onModalClose(): void;
}

function ManageApplicationsModal(props: ManageApplicationsModalProps) {
  const { isOpen, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ManageApplicationsModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default ManageApplicationsModal;
