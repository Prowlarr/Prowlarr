import React from 'react';
import { Application } from 'Settings/Applications/Application';
import ProviderSettingsModal from 'Settings/Applications/ProviderSettingsModal';

interface ListenarrProps {
  selectedApplication: Application;
  onModalClose: () => void;
}

function Listenarr({ selectedApplication, onModalClose }: ListenarrProps) {
  return (
    <ProviderSettingsModal
      providerData={selectedApplication}
      section="applications"
      onModalClose={onModalClose}
    />
  );
}

export default Listenarr;
