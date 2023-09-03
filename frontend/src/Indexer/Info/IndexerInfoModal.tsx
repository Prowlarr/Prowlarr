import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import IndexerInfoModalContent from './IndexerInfoModalContent';

interface IndexerInfoModalProps {
  isOpen: boolean;
  indexerId: number;
  onModalClose(): void;
  onCloneIndexerPress(id: number): void;
}

function IndexerInfoModal(props: IndexerInfoModalProps) {
  const { isOpen, indexerId, onModalClose, onCloneIndexerPress } = props;

  return (
    <Modal size={sizes.LARGE} isOpen={isOpen} onModalClose={onModalClose}>
      <IndexerInfoModalContent
        indexerId={indexerId}
        onModalClose={onModalClose}
        onCloneIndexerPress={onCloneIndexerPress}
      />
    </Modal>
  );
}

export default IndexerInfoModal;
