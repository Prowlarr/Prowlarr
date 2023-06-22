import PropTypes from 'prop-types';
import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import Link from 'Components/Link/Link';
import translate from 'Utilities/String/translate';
import styles from './HistoryDetails.css';

function HistoryDetails(props) {
  const {
    indexer,
    eventType,
    data
  } = props;

  if (eventType === 'indexerQuery' || eventType === 'indexerRss') {
    const {
      query,
      queryResults,
      categories,
      source,
      url
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
              title={translate('QueryResults')}
              data={queryResults ? queryResults : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={translate('Categories')}
              data={categories ? categories : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={translate('Source')}
              data={source}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={translate('Url')}
              data={url ? <Link to={url}>{translate('Link')}</Link> : '-'}
            />
        }
      </DescriptionList>
    );
  }

  if (eventType === 'releaseGrabbed') {
    const {
      source,
      grabTitle,
      url
    } = data;

    return (
      <DescriptionList>
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
              title={translate('Source')}
              data={source ? source : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={translate('GrabTitle')}
              data={grabTitle ? grabTitle : '-'}
            />
        }

        {
          !!data &&
            <DescriptionListItem
              title={translate('Url')}
              data={url ? <Link to={url}>{translate('Link')}</Link> : '-'}
            />
        }
      </DescriptionList>
    );
  }

  if (eventType === 'indexerAuth') {
    return (
      <DescriptionList
        descriptionClassName={styles.description}
        title={translate('Auth')}
      >
        {
          !!indexer &&
            <DescriptionListItem
              title={translate('Indexer')}
              data={indexer.name}
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
