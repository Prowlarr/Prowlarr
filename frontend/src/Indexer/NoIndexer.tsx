import React from 'react';
import Alert from 'Components/Alert';
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
      <Alert kind={kinds.WARNING} className={styles.message}>
        {translate('AllIndexersHiddenDueToFilter')}
      </Alert>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        No indexers found, to get started you'll want to add a new indexer.
      </div>

      <div className={styles.buttonContainer}>
        <Button kind={kinds.PRIMARY} onPress={onAddIndexerPress}>
          {translate('AddNewIndexer')}
        </Button>
      </div>
    </div>
  );
}

export default NoIndexer;
