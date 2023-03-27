import React from 'react';
import Modal from 'Components/Modal/Modal';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import { sizes } from 'Helpers/Props';
import OverrideMatchModalContent from './OverrideMatchModalContent';

interface OverrideMatchModalProps {
  isOpen: boolean;
  title: string;
  indexerId: number;
  guid: string;
  protocol: DownloadProtocol;
  isGrabbing: boolean;
  grabError?: string;
  onModalClose(): void;
}

function OverrideMatchModal(props: OverrideMatchModalProps) {
  const {
    isOpen,
    title,
    indexerId,
    guid,
    protocol,
    isGrabbing,
    grabError,
    onModalClose,
  } = props;

  return (
    <Modal isOpen={isOpen} size={sizes.LARGE} onModalClose={onModalClose}>
      <OverrideMatchModalContent
        title={title}
        indexerId={indexerId}
        guid={guid}
        protocol={protocol}
        isGrabbing={isGrabbing}
        grabError={grabError}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default OverrideMatchModal;
