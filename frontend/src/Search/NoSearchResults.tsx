import React from 'react';
import translate from 'Utilities/String/translate';
import styles from './NoSearchResults.css';

interface NoSearchResultsProps {
  totalItems: number;
}

function NoSearchResults(props: NoSearchResultsProps) {
  const { totalItems } = props;

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
      <div className={styles.message}>{translate('NoSearchResultsFound')}</div>
    </div>
  );
}

export default NoSearchResults;
