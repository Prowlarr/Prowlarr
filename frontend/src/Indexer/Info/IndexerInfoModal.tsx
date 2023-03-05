import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import IndexerInfoModalContent from './IndexerInfoModalContent';

interface IndexerInfoModalProps {
  isOpen: boolean;
  indexerId: number;
  onModalClose(): void;
}

function IndexerInfoModal(props: IndexerInfoModalProps) {
  const { isOpen, onModalClose, indexerId } = props;

  return (
    <Modal size={sizes.MEDIUM} isOpen={isOpen} onModalClose={onModalClose}>
      <IndexerInfoModalContent
        indexerId={indexerId}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default IndexerInfoModal;
