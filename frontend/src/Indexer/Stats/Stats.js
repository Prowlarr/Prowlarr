import PropTypes from 'prop-types';
import React from 'react';
import BarChart from 'Components/Chart/BarChart';
import DoughnutChart from 'Components/Chart/DoughnutChart';
import StackedBarChart from 'Components/Chart/StackedBarChart';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import { align, kinds } from 'Helpers/Props';
import getErrorMessage from 'Utilities/Object/getErrorMessage';
import StatsFilterMenu from './StatsFilterMenu';
import styles from './Stats.css';

function getAverageResponseTimeData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.indexerName,
      value: indexer.averageResponseTime
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getFailureRateData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.indexerName,
      value: (indexer.numberOfFailedQueries + indexer.numberOfFailedRssQueries + indexer.numberOfFailedAuthQueries + indexer.numberOfFailedGrabs) /
        (indexer.numberOfQueries + indexer.numberOfRssQueries + indexer.numberOfAuthQueries + indexer.numberOfGrabs)
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getTotalRequestsData(indexerStats) {
  const data = {
    labels: indexerStats.map((indexer) => indexer.indexerName),
    datasets: [
      {
        label: 'Search Queries',
        data: indexerStats.map((indexer) => indexer.numberOfQueries)
      },
      {
        label: 'Rss Queries',
        data: indexerStats.map((indexer) => indexer.numberOfRssQueries)
      },
      {
        label: 'Auth Queries',
        data: indexerStats.map((indexer) => indexer.numberOfAuthQueries)
      }
    ]
  };

  return data;
}

function getNumberGrabsData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.indexerName,
      value: indexer.numberOfGrabs - indexer.numberOfFailedGrabs
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getUserAgentGrabsData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.userAgent ? indexer.userAgent : 'Other',
      value: indexer.numberOfGrabs
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getUserAgentQueryData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.userAgent ? indexer.userAgent : 'Other',
      value: indexer.numberOfQueries
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getHostGrabsData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.host ? indexer.host : 'Other',
      value: indexer.numberOfGrabs
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function getHostQueryData(indexerStats) {
  const data = indexerStats.map((indexer) => {
    return {
      label: indexer.host ? indexer.host : 'Other',
      value: indexer.numberOfQueries
    };
  });

  data.sort((a, b) => {
    return b.value - a.value;
  });

  return data;
}

function Stats(props) {
  const {
    item,
    isFetching,
    isPopulated,
    error,
    filters,
    selectedFilterKey,
    onFilterSelect
  } = props;

  const isLoaded = !!(!error && isPopulated);

  return (
    <PageContent>
      <PageToolbar>
        <PageToolbarSection
          alignContent={align.RIGHT}
          collapseButtons={false}
        >
          <StatsFilterMenu
            selectedFilterKey={selectedFilterKey}
            filters={filters}
            onFilterSelect={onFilterSelect}
            isDisabled={false}
          />
        </PageToolbarSection>
      </PageToolbar>
      <PageContentBody>
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
                  data={getAverageResponseTimeData(item.indexers)}
                  title='Average Response Times (ms)'
                />
              </div>
              <div className={styles.fullWidthChart}>
                <BarChart
                  data={getFailureRateData(item.indexers)}
                  title='Indexer Failure Rate'
                  kind={kinds.WARNING}
                />
              </div>
              <div className={styles.halfWidthChart}>
                <StackedBarChart
                  data={getTotalRequestsData(item.indexers)}
                  title='Total Indexer Queries'
                />
              </div>
              <div className={styles.halfWidthChart}>
                <BarChart
                  data={getNumberGrabsData(item.indexers)}
                  title='Total Indexer Successful Grabs'
                />
              </div>
              <div className={styles.halfWidthChart}>
                <BarChart
                  data={getUserAgentQueryData(item.userAgents)}
                  title='Total User Agent Queries'
                  horizontal={true}
                />
              </div>
              <div className={styles.halfWidthChart}>
                <BarChart
                  data={getUserAgentGrabsData(item.userAgents)}
                  title='Total User Agent Grabs'
                  horizontal={true}
                />
              </div>
              <div className={styles.halfWidthChart}>
                <DoughnutChart
                  data={getHostQueryData(item.hosts)}
                  title='Total Host Queries'
                  horizontal={true}
                />
              </div>
              <div className={styles.halfWidthChart}>
                <DoughnutChart
                  data={getHostGrabsData(item.hosts)}
                  title='Total Host Grabs'
                  horizontal={true}
                />
              </div>
            </div>
        }
      </PageContentBody>
    </PageContent>
  );
}

Stats.propTypes = {
  item: PropTypes.object.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  filters: PropTypes.arrayOf(PropTypes.object).isRequired,
  selectedFilterKey: PropTypes.string.isRequired,
  customFilters: PropTypes.arrayOf(PropTypes.object).isRequired,
  onFilterSelect: PropTypes.func.isRequired,
  error: PropTypes.object,
  data: PropTypes.object
};

export default Stats;
