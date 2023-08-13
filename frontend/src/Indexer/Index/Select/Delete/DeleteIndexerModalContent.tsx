import { orderBy } from 'lodash';
import React, { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import Indexer from 'Indexer/Indexer';
import { bulkDeleteIndexers } from 'Store/Actions/indexerActions';
import createAllIndexersSelector from 'Store/Selectors/createAllIndexersSelector';
import translate from 'Utilities/String/translate';
import styles from './DeleteIndexerModalContent.css';

interface DeleteIndexerModalContentProps {
  indexerIds: number[];
  onModalClose(): void;
}

function DeleteIndexerModalContent(props: DeleteIndexerModalContentProps) {
  const { indexerIds, onModalClose } = props;

  const allIndexers: Indexer[] = useSelector(createAllIndexersSelector());
  const dispatch = useDispatch();

  const indexers = useMemo((): Indexer[] => {
    const indexerList = indexerIds.map((id) => {
      return allIndexers.find((s) => s.id === id);
    }) as Indexer[];

    return orderBy(indexerList, ['sortName']);
  }, [indexerIds, allIndexers]);

  const onDeleteIndexerConfirmed = useCallback(() => {
    dispatch(
      bulkDeleteIndexers({
        ids: indexerIds,
      })
    );

    onModalClose();
  }, [indexerIds, dispatch, onModalClose]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('DeleteSelectedIndexers')}</ModalHeader>

      <ModalBody>
        <div className={styles.message}>
          {translate('DeleteSelectedIndexersMessageText', {
            count: indexers.length,
          })}
        </div>

        <ul>
          {indexers.map((s) => {
            return (
              <li key={s.id}>
                <span>{s.name}</span>
              </li>
            );
          })}
        </ul>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.DANGER} onPress={onDeleteIndexerConfirmed}>
          {translate('Delete')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default DeleteIndexerModalContent;
