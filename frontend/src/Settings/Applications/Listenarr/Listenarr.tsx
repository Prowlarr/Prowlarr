import React from 'react';
import ProviderSettingsModal from 'Settings/Applications/ProviderSettingsModal';
import Application from 'typings/Application';

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
