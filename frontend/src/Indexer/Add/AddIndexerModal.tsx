import React, { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearIndexerSchema } from 'Store/Actions/indexerActions';
import AddIndexerModalContent from './AddIndexerModalContent';
import styles from './AddIndexerModal.css';

interface AddIndexerModalProps {
  isOpen: boolean;
  onSelectIndexer(): void;
  onModalClose(): void;
}

function AddIndexerModal({
  isOpen,
  onSelectIndexer,
  onModalClose,
  ...otherProps
}: AddIndexerModalProps) {
  const dispatch = useDispatch();

  const onModalClosePress = useCallback(() => {
    dispatch(clearIndexerSchema());
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      className={styles.modal}
      onModalClose={onModalClosePress}
    >
      <AddIndexerModalContent
        {...otherProps}
        onSelectIndexer={onSelectIndexer}
        onModalClose={onModalClosePress}
      />
    </Modal>
  );
}

export default AddIndexerModal;
