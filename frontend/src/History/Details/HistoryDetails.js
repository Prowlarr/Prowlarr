import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';
import styles from './HistoryDetails.css';

function HistoryDetails(props) {
  const {
    indexer,
    eventType,
    data
  } = props;

  if (eventType === 'indexerQuery') {
    const {
      query,
      queryResults,
      categories,
      source
    } = data;

    return (
      <DescriptionList>
        <DescriptionListItem
          descriptionClassName={styles.description}
          title={translate('Query')}
          data={query ? query : '-'}
        />

        {
          !!indexer &&
            <DescriptionListItem
              title={translate('Indexer')}
              data={indexer.name}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={'Query Results'}
              data={queryResults ? queryResults : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={'Categories'}
              data={categories ? categories : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={'Source'}
              data={source}
            />
        }
      </DescriptionList>
    );
  }

  return (
    <DescriptionList>
      <DescriptionListItem
        descriptionClassName={styles.description}
        title={translate('Name')}
        data={data.query}
      />
    </DescriptionList>
  );
}

HistoryDetails.propTypes = {
  indexer: PropTypes.object.isRequired,
  eventType: PropTypes.string.isRequired,
  data: PropTypes.object.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default HistoryDetails;
