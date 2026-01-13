import React from 'react';
import Application from 'typings/Application';
import EditApplicationModalConnector from './Applications/EditApplicationModalConnector';

interface ProviderSettingsModalProps {
  providerData: Application;
  section?: string;
  onModalClose: () => void;
}

function ProviderSettingsModal({
  providerData,
  onModalClose,
}: ProviderSettingsModalProps) {
  return (
    <EditApplicationModalConnector
      isOpen={true}
      item={providerData}
      onModalClose={onModalClose}
    />
  );
}

export default ProviderSettingsModal;
