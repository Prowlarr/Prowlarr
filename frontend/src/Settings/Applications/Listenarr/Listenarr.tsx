import React from 'react';
import EditApplicationModalConnector from 'Settings/Applications/Applications/EditApplicationModalConnector';
import Application from 'typings/Application';

interface ListenarrProps {
  selectedApplication: Application;
  onModalClose: () => void;
}

function Listenarr({ selectedApplication, onModalClose }: ListenarrProps) {
  return (
    <EditApplicationModalConnector
      isOpen={true}
      item={selectedApplication}
      onModalClose={onModalClose}
    />
  );
}

export default Listenarr;
