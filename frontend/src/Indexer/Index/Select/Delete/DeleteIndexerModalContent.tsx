import { sortBy } from 'lodash-es';
import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import { bulkDeleteIndexers } from 'Store/Actions/indexerIndexActions';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import styles from './DeleteIndexerModalContent.css';

interface DeleteIndexerModalContentProps {
  indexerIds: number[];
  onModalClose(): void;
}

function DeleteIndexerModalContent(props: DeleteIndexerModalContentProps) {
  const { indexerIds, onModalClose } = props;

  const allIndexer = useSelector(createAllIndexersSelector());
  const dispatch = useDispatch();

  const indexers = useMemo(() => {
    const indexers = indexerIds.map((id) => {
      return allIndexer.find((s) => s.id === id);
    });

    return sortBy(indexers, ['sortTitle']);
  }, [indexerIds, allIndexer]);

  const onDeleteIndexerConfirmed = useCallback(() => {
    dispatch(
      bulkDeleteIndexers({
        indexerIds,
      })
    );

    onModalClose();
  }, [indexerIds, dispatch, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>Delete Selected Indexer</ModalHeader>

      <ModalBody>
        <div className={styles.message}>
          {`Are you sure you want to delete ${indexers.length} selected indexers?`}
        </div>

        <ul>
          {indexers.map((s) => {
            return (
              <li key={s.name}>
                <span>{s.name}</span>
              </li>
            );
          })}
        </ul>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>Cancel</Button>

        <Button kind={kinds.DANGER} onPress={onDeleteIndexerConfirmed}>
          Delete
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteIndexerModalContent;
