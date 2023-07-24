import React from 'react';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoIndexer.css';

interface NoIndexerProps {
  totalItems: number;
  onAddIndexerPress(): void;
}

function NoIndexer(props: NoIndexerProps) {
  const { totalItems, onAddIndexerPress } = props;

  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {translate('AllIndexersHiddenDueToFilter')}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        No indexers found, to get started you'll want to add a new indexer.
      </div>

      <div className={styles.buttonContainer}>
        <Button onPress={onAddIndexerPress} kind={kinds.PRIMARY}>
          {translate('AddNewIndexer')}
        </Button>
      </div>
    </div>
  );
}

export default NoIndexer;
