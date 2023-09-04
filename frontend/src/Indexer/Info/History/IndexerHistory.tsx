import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { IndexerHistoryAppState } from 'App/State/IndexerAppState';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds } from 'Helpers/Props';
import Indexer from 'Indexer/Indexer';
import {
  clearIndexerHistory,
  fetchIndexerHistory,
} from 'Store/Actions/indexerHistoryActions';
import { createIndexerSelectorForHook } from 'Store/Selectors/createIndexerSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import translate from 'Utilities/String/translate';
import IndexerHistoryRow from './IndexerHistoryRow';

const columns = [
  {
    name: 'eventType',
    isVisible: true,
  },
  {
    name: 'query',
    label: () => translate('Query'),
    isVisible: true,
  },
  {
    name: 'parameters',
    label: () => translate('Parameters'),
    isVisible: true,
  },
  {
    name: 'date',
    label: () => translate('Date'),
    isVisible: true,
  },
  {
    name: 'source',
    label: () => translate('Source'),
    isVisible: true,
  },
  {
    name: 'details',
    label: () => translate('Details'),
    isVisible: true,
  },
];

function createIndexerHistorySelector() {
  return createSelector(
    (state: AppState) => state.indexerHistory,
    createUISettingsSelector(),
    (state: AppState) => state.history.pageSize,
    (indexerHistory: IndexerHistoryAppState, uiSettings, pageSize) => {
      return {
        ...indexerHistory,
        shortDateFormat: uiSettings.shortDateFormat,
        timeFormat: uiSettings.timeFormat,
        pageSize,
      };
    }
  );
}

interface IndexerHistoryProps {
  indexerId: number;
}

function IndexerHistory(props: IndexerHistoryProps) {
  const {
    isFetching,
    isPopulated,
    error,
    items,
    shortDateFormat,
    timeFormat,
    pageSize,
  } = useSelector(createIndexerHistorySelector());

  const indexer = useSelector(
    createIndexerSelectorForHook(props.indexerId)
  ) as Indexer;

  const dispatch = useDispatch();

  useEffect(() => {
    dispatch(
      fetchIndexerHistory({ indexerId: props.indexerId, limit: pageSize })
    );

    return () => {
      dispatch(clearIndexerHistory());
    };
  }, [props, pageSize, dispatch]);

  const hasItems = !!items.length;

  if (isFetching) {
    return <LoadingIndicator />;
  }

  if (!isFetching && !!error) {
    return (
      <Alert kind={kinds.DANGER}>{translate('IndexerHistoryLoadError')}</Alert>
    );
  }

  if (isPopulated && !hasItems && !error) {
    return <Alert kind={kinds.INFO}>{translate('NoIndexerHistory')}</Alert>;
  }

  if (isPopulated && hasItems && !error) {
    return (
      <Table columns={columns}>
        <TableBody>
          {items.map((item) => {
            return (
              <IndexerHistoryRow
                key={item.id}
                indexer={indexer}
                shortDateFormat={shortDateFormat}
                timeFormat={timeFormat}
                {...item}
              />
            );
          })}
        </TableBody>
      </Table>
    );
  }

  return null;
}

export default IndexerHistory;
