import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import EditIndexerModalContentConnector from './EditIndexerModalContentConnector';

function EditIndexerModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      size={sizes.LARGE}
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <EditIndexerModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

EditIndexerModal.propTypes = {
  ...EditIndexerModalContentConnector.propTypes,
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default EditIndexerModal;
