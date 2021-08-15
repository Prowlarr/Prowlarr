import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import AddIndexerModalContent from './AddIndexerModalContent';
import styles from './AddIndexerModal.css';

function AddIndexerModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
      className={styles.modal}
    >
      <AddIndexerModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

AddIndexerModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default AddIndexerModal;
