import PropTypes from 'prop-types';
import React from 'react';
import translate from 'Utilities/String/translate';
import styles from './NoSearchResults.css';

function NoSearchResults(props) {
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
      <div className={styles.message}>
        {translate('NoSearchResultsFound')}
      </div>
    </div>
  );
}

NoSearchResults.propTypes = {
  totalItems: PropTypes.number.isRequired
};

export default NoSearchResults;
