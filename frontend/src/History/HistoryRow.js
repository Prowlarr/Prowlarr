import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons, kinds } from 'Helpers/Props';
import CapabilitiesLabel from 'Indexer/Index/Table/CapabilitiesLabel';
import translate from 'Utilities/String/translate';
import HistoryDetailsModal from './Details/HistoryDetailsModal';
import * as historyDataTypes from './historyDataTypes';
import HistoryEventTypeCell from './HistoryEventTypeCell';
import HistoryRowParameter from './HistoryRowParameter';
import styles from './HistoryRow.css';

const historyParameters = [
  { key: historyDataTypes.IMDB_ID, title: 'IMDb' },
  { key: historyDataTypes.TMDB_ID, title: 'TMDb' },
  { key: historyDataTypes.TVDB_ID, title: 'TVDb' },
  { key: historyDataTypes.TRAKT_ID, title: 'Trakt' },
  { key: historyDataTypes.R_ID, title: 'TvRage' },
  { key: historyDataTypes.TVMAZE_ID, title: 'TvMaze' },
  { key: historyDataTypes.SEASON, title: () => translate('Season') },
  { key: historyDataTypes.EPISODE, title: () => translate('Episode') },
  { key: historyDataTypes.ARTIST, title: () => translate('Artist') },
  { key: historyDataTypes.ALBUM, title: () => translate('Album') },
  { key: historyDataTypes.LABEL, title: () => translate('Label') },
  { key: historyDataTypes.TRACK, title: () => translate('Track') },
  { key: historyDataTypes.YEAR, title: () => translate('Year') },
  { key: historyDataTypes.GENRE, title: () => translate('Genre') },
  { key: historyDataTypes.AUTHOR, title: () => translate('Author') },
  { key: historyDataTypes.TITLE, title: () => translate('Title') },
  { key: historyDataTypes.PUBLISHER, title: () => translate('Publisher') }
];

class HistoryRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isDetailsModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    if (
      prevProps.isMarkingAsFailed &&
      !this.props.isMarkingAsFailed &&
      !this.props.markAsFailedError
    ) {
      this.setState({ isDetailsModalOpen: false });
    }
  }

  //
  // Listeners

  onSearchPress = () => {
    const {
      indexer,
      data
    } = this.props;

    const { query, queryType, limit, offset } = data;

    let searchQuery = query;
    let categories = [];

    if (data.categories) {
      categories = data.categories.split(',').map((item) => parseInt(item));
    }

    const searchParams = [
      historyDataTypes.IMDB_ID,
      historyDataTypes.TMDB_ID,
      historyDataTypes.TVDB_ID,
      historyDataTypes.TRAKT_ID,
      historyDataTypes.R_ID,
      historyDataTypes.TVMAZE_ID,
      historyDataTypes.SEASON,
      historyDataTypes.EPISODE,
      historyDataTypes.ARTIST,
      historyDataTypes.ALBUM,
      historyDataTypes.LABEL,
      historyDataTypes.TRACK,
      historyDataTypes.YEAR,
      historyDataTypes.GENRE,
      historyDataTypes.AUTHOR,
      historyDataTypes.TITLE,
      historyDataTypes.PUBLISHER
    ]
      .reduce((acc, key) => {
        if (key in data && data[key].length > 0) {
          const value = data[key];

          acc.push({ key, value });
        }

        return acc;
      }, [])
      .map((item) => `{${item.key}:${item.value}}`)
      .join('')
    ;

    if (searchParams.length > 0) {
      searchQuery += `${searchParams}`;
    }

    this.props.onSearchPress(searchQuery, indexer.id, categories, queryType, parseInt(limit), parseInt(offset));
  };

  onDetailsPress = () => {
    this.setState({ isDetailsModalOpen: true });
  };

  onDetailsModalClose = () => {
    this.setState({ isDetailsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      indexer,
      eventType,
      date,
      data,
      successful,
      isMarkingAsFailed,
      columns,
      shortDateFormat,
      timeFormat,
      onMarkAsFailedPress
    } = this.props;

    if (!indexer) {
      return null;
    }

    const parameters = historyParameters.filter((parameter) => parameter.key in data && data[parameter.key]);

    return (
      <TableRow>
        {
          columns.map((column) => {
            const {
              name,
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (name === 'eventType') {
              return (
                <HistoryEventTypeCell
                  key={name}
                  indexer={indexer}
                  eventType={eventType}
                  data={data}
                  successful={successful}
                />
              );
            }

            if (name === 'indexer') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexer}
                >
                  {indexer.name}
                </TableRowCell>
              );
            }

            if (name === 'query') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.query}
                >
                  {data.query}
                </TableRowCell>
              );
            }

            if (name === 'parameters') {
              return (
                <TableRowCell key={name}>
                  <div className={styles.parametersContent}>
                    {parameters.map((parameter) => {
                      return (
                        <HistoryRowParameter
                          key={parameter.key}
                          title={parameter.title}
                          value={data[parameter.key]}
                        />
                      );
                    }
                    )}
                  </div>
                </TableRowCell>
              );
            }

            if (name === 'grabTitle') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexer}
                >
                  {
                    data.grabTitle ?
                      data.grabTitle :
                      null
                  }
                </TableRowCell>
              );
            }

            if (name === 'queryType') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.query}
                >
                  {
                    data.queryType ?
                      <Label kind={kinds.INFO}>
                        {data.queryType}
                      </Label> :
                      null
                  }
                </TableRowCell>
              );
            }

            if (name === 'categories') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexer}
                >
                  {
                    data.categories ?
                      <CapabilitiesLabel
                        capabilities={indexer.capabilities}
                        categoryFilter={data.categories.split(',').map(Number)}
                      /> :
                      null
                  }
                </TableRowCell>
              );
            }

            if (name === 'source') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.indexer}
                >
                  {
                    data.source ?
                      data.source :
                      null
                  }
                </TableRowCell>
              );
            }

            if (name === 'elapsedTime') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.elapsedTime}
                >
                  {
                    data.elapsedTime ?
                      `${data.elapsedTime}ms` :
                      null
                  }
                  {
                    data.cached === '1' ?
                      ' (cached)' :
                      null
                  }
                </TableRowCell>
              );
            }

            if (name === 'date') {
              return (
                <RelativeDateCellConnector
                  key={name}
                  date={date}
                  className={styles.date}
                />
              );
            }

            if (name === 'details') {
              return (
                <TableRowCell
                  key={name}
                  className={styles.details}
                >
                  <IconButton
                    name={icons.INFO}
                    onPress={this.onDetailsPress}
                    title={translate('HistoryDetails')}
                  />

                  {
                    eventType === 'indexerQuery' ?
                      <IconButton
                        name={icons.SEARCH}
                        onPress={this.onSearchPress}
                        title={translate('RepeatSearch')}
                      /> :
                      null
                  }
                </TableRowCell>
              );
            }

            return null;
          })
        }

        <HistoryDetailsModal
          isOpen={this.state.isDetailsModalOpen}
          eventType={eventType}
          data={data}
          indexer={indexer}
          isMarkingAsFailed={isMarkingAsFailed}
          shortDateFormat={shortDateFormat}
          timeFormat={timeFormat}
          onMarkAsFailedPress={onMarkAsFailedPress}
          onModalClose={this.onDetailsModalClose}
        />
      </TableRow>
    );
  }

}

HistoryRow.propTypes = {
  indexerId: PropTypes.number,
  indexer: PropTypes.object.isRequired,
  eventType: PropTypes.string.isRequired,
  successful: PropTypes.bool.isRequired,
  date: PropTypes.string.isRequired,
  data: PropTypes.object.isRequired,
  isMarkingAsFailed: PropTypes.bool,
  markAsFailedError: PropTypes.object,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  onMarkAsFailedPress: PropTypes.func.isRequired,
  onSearchPress: PropTypes.func.isRequired
};

export default HistoryRow;
