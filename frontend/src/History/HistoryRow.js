import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons } from 'Helpers/Props';
import CapabilitiesLabel from 'Indexer/Index/Table/CapabilitiesLabel';
import HistoryDetailsModal from './Details/HistoryDetailsModal';
import HistoryEventTypeCell from './HistoryEventTypeCell';
import HistoryRowParameter from './HistoryRowParameter';
import styles from './HistoryRow.css';

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

    let categories = [];

    if (data.categories) {
      categories = data.categories.split(',').map((item) => {
        return parseInt(item);
      });
    }

    this.props.onSearchPress(data.query, indexer.id, categories);
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
                <TableRowCell
                  key={name}
                  className={styles.parameters}
                >
                  {
                    data.imdbId ?
                      <HistoryRowParameter
                        title='IMDb'
                        value={data.imdbId}
                      /> :
                      null
                  }

                  {
                    data.tmdbId ?
                      <HistoryRowParameter
                        title='TMDb'
                        value={data.tmdbId}
                      /> :
                      null
                  }

                  {
                    data.tvdbId ?
                      <HistoryRowParameter
                        title='TVDb'
                        value={data.tvdbId}
                      /> :
                      null
                  }

                  {
                    data.traktId ?
                      <HistoryRowParameter
                        title='Trakt'
                        value={data.traktId}
                      /> :
                      null
                  }

                  {
                    data.rId ?
                      <HistoryRowParameter
                        title='TvRage'
                        value={data.rId}
                      /> :
                      null
                  }

                  {
                    data.tvMazeId ?
                      <HistoryRowParameter
                        title='TvMaze'
                        value={data.tvMazeId}
                      /> :
                      null
                  }

                  {
                    data.season ?
                      <HistoryRowParameter
                        title='Season'
                        value={data.season}
                      /> :
                      null
                  }

                  {
                    data.episode ?
                      <HistoryRowParameter
                        title='Episode'
                        value={data.episode}
                      /> :
                      null
                  }

                  {
                    data.artist ?
                      <HistoryRowParameter
                        title='Artist'
                        value={data.artist}
                      /> :
                      null
                  }

                  {
                    data.album ?
                      <HistoryRowParameter
                        title='Album'
                        value={data.album}
                      /> :
                      null
                  }

                  {
                    data.author ?
                      <HistoryRowParameter
                        title='Author'
                        value={data.author}
                      /> :
                      null
                  }

                  {
                    data.bookTitle ?
                      <HistoryRowParameter
                        title='Book'
                        value={data.bookTitle}
                      /> :
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
                  {
                    eventType === 'indexerQuery' ?
                      <IconButton
                        name={icons.SEARCH}
                        onPress={this.onSearchPress}
                        title='Repeat Search'
                      /> :
                      null
                  }
                  <IconButton
                    name={icons.INFO}
                    onPress={this.onDetailsPress}
                    title='History Details'
                  />
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
