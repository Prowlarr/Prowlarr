import React from 'react';
import Modal from 'Components/Modal/Modal';
import ManageApplicationsEditModalContent from './ManageApplicationsEditModalContent';

interface ManageApplicationsEditModalProps {
  isOpen: boolean;
  applicationIds: number[];
  onSavePress(payload: object): void;
  onModalClose(): void;
}

function ManageApplicationsEditModal(props: ManageApplicationsEditModalProps) {
  const { isOpen, applicationIds, onSavePress, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <ManageApplicationsEditModalContent
        applicationIds={applicationIds}
        onSavePress={onSavePress}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default ManageApplicationsEditModal;
