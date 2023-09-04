import React, { useCallback, useState } from 'react';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons } from 'Helpers/Props';
import HistoryDetailsModal from 'History/Details/HistoryDetailsModal';
import HistoryEventTypeCell from 'History/HistoryEventTypeCell';
import { historyParameters } from 'History/HistoryRow';
import HistoryRowParameter from 'History/HistoryRowParameter';
import Indexer from 'Indexer/Indexer';
import { HistoryData } from 'typings/History';
import translate from 'Utilities/String/translate';
import styles from './IndexerHistoryRow.css';

interface IndexerHistoryRowProps {
  data: HistoryData;
  date: string;
  eventType: string;
  successful: boolean;
  indexer: Indexer;
  shortDateFormat: string;
  timeFormat: string;
}

function IndexerHistoryRow(props: IndexerHistoryRowProps) {
  const {
    data,
    date,
    eventType,
    successful,
    indexer,
    shortDateFormat,
    timeFormat,
  } = props;

  const [isDetailsModalOpen, setIsDetailsModalOpen] = useState(false);

  const onDetailsModalPress = useCallback(() => {
    setIsDetailsModalOpen(true);
  }, [setIsDetailsModalOpen]);

  const onDetailsModalClose = useCallback(() => {
    setIsDetailsModalOpen(false);
  }, [setIsDetailsModalOpen]);

  const parameters = historyParameters.filter(
    (parameter) =>
      parameter.key in data && data[parameter.key as keyof HistoryData]
  );

  return (
    <TableRow>
      <HistoryEventTypeCell
        indexer={indexer}
        eventType={eventType}
        data={data}
        successful={successful}
      />

      <TableRowCell className={styles.query}>{data.query}</TableRowCell>

      <TableRowCell>
        <div className={styles.parametersContent}>
          {parameters.map((parameter) => {
            return (
              <HistoryRowParameter
                key={parameter.key}
                title={parameter.title}
                value={data[parameter.key as keyof HistoryData]}
              />
            );
          })}
        </div>
      </TableRowCell>

      <RelativeDateCell date={date} />

      <TableRowCell className={styles.source}>
        {data.source ? data.source : null}
      </TableRowCell>

      <TableRowCell className={styles.details}>
        <IconButton
          name={icons.INFO}
          onPress={onDetailsModalPress}
          title={translate('HistoryDetails')}
        />
      </TableRowCell>

      <HistoryDetailsModal
        isOpen={isDetailsModalOpen}
        eventType={eventType}
        data={data}
        indexer={indexer}
        shortDateFormat={shortDateFormat}
        timeFormat={timeFormat}
        onModalClose={onDetailsModalClose}
      />
    </TableRow>
  );
}

export default IndexerHistoryRow;
