import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import AddIndexerModalContentConnector from './AddIndexerModalContentConnector';
import styles from './AddIndexerModal.css';

function AddIndexerModal({ isOpen, onModalClose, onSelectIndexer, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={onModalClose}
      className={styles.modal}
    >
      <AddIndexerModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
        onSelectIndexer={onSelectIndexer}
      />
    </Modal>
  );
}

AddIndexerModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired,
  onSelectIndexer: PropTypes.func.isRequired
};

export default AddIndexerModal;
