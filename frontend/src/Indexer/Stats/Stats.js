import PropTypes from 'prop-types';
import React from 'react';
import BarChart from 'Components/Chart/BarChart';
import DoughnutChart from 'Components/Chart/DoughnutChart';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import styles from './Stats.css';

function getAverageResponseTimeData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.indexerName,
      value: indexer.averageResponseTime
    };
  });

  return data;
}

function getTotalRequestsData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.indexerName,
      value: indexer.numberOfQueries
    };
  });

  return data;
}

function Stats(props) {
  const {
    items,
    isFetching,
    isPopulated,
    error
  } = props;

  const isLoaded = !!(!error && isPopulated && items.length);

  return (
    <PageContent>
      <PageToolbar />
      {
        isFetching && !isPopulated &&
          <LoadingIndicator />
      }

      {
        !isFetching && !!error &&
          <div className={styles.errorMessage}>
            {getErrorMessage(error, 'Failed to load indexer stats from API')}
          </div>
      }

      {
        isLoaded &&
          <div>
            <div className={styles.fullWidthChart}>
              <BarChart
                data={getAverageResponseTimeData(items)}
                title='Average Response Times (ms)'
              />
            </div>
            <div className={styles.halfWidthChart}>
              <DoughnutChart
                data={getTotalRequestsData(items)}
                title='Total Indexer Queries'
              />
            </div>
          </div>
      }
    </PageContent>
  );
}

Stats.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  data: PropTypes.object
};

export default Stats;
