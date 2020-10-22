import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import DeleteIndexerModalContentConnector from './DeleteIndexerModalContentConnector';

function DeleteIndexerModal(props) {
  const {
    isOpen,
    onModalClose,
    ...otherProps
  } = props;

  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <DeleteIndexerModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

DeleteIndexerModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default DeleteIndexerModal;
