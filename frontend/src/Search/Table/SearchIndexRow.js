import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatAge from 'Utilities/Number/formatAge';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import CategoryLabel from './CategoryLabel';
import Peers from './Peers';
import ProtocolLabel from './ProtocolLabel';
import styles from './SearchIndexRow.css';

class SearchIndexRow extends Component {

  //
  // Render

  render() {
    const {
      protocol,
      categories,
      age,
      ageHours,
      ageMinutes,
      publishDate,
      title,
      infoUrl,
      downloadUrl,
      indexer,
      size,
      files,
      grabs,
      seeders,
      leechers,
      indexerFlags,
      columns,
      longDateFormat,
      timeFormat
    } = this.props;

    return (
      <>
        {
          columns.map((column) => {
            const {
              isVisible
            } = column;

            if (!isVisible) {
              return null;
            }

            if (column.name === 'protocol') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <ProtocolLabel
                    protocol={protocol}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'age') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                  title={formatDateTime(publishDate, longDateFormat, timeFormat, { includeSeconds: true })}
                >
                  {formatAge(age, ageHours, ageMinutes)}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'title') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <Link
                    to={infoUrl}
                    title={title}
                  >
                    <div>
                      {title}
                    </div>
                  </Link>
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'indexer') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {indexer}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'size') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {formatBytes(size)}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'files') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {files}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'grabs') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {grabs}
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'peers') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {
                    protocol === 'torrent' &&
                      <Peers
                        seeders={seeders}
                        leechers={leechers}
                      />
                  }
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'category') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <CategoryLabel
                    categories={categories}
                  />
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'indexerFlags') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  {
                    !!indexerFlags.length &&
                      <Popover
                        anchor={
                          <Icon
                            name={icons.FLAG}
                            kind={kinds.PRIMARY}
                          />
                        }
                        title={translate('IndexerFlags')}
                        body={
                          <ul>
                            {
                              indexerFlags.map((flag, index) => {
                                return (
                                  <li key={index}>
                                    {flag}
                                  </li>
                                );
                              })
                            }
                          </ul>
                        }
                        position={tooltipPositions.LEFT}
                      />
                  }
                </VirtualTableRowCell>
              );
            }

            if (column.name === 'actions') {
              return (
                <VirtualTableRowCell
                  key={column.name}
                  className={styles[column.name]}
                >
                  <IconButton
                    className={styles.downloadLink}
                    name={icons.DOWNLOAD}
                    title={'Grab'}
                    to={downloadUrl}
                  />

                  <IconButton
                    className={styles.downloadLink}
                    name={icons.SAVE}
                    title={'Save'}
                    to={downloadUrl}
                  />
                </VirtualTableRowCell>
              );
            }

            return null;
          })
        }
      </>
    );
  }
}

SearchIndexRow.propTypes = {
  guid: PropTypes.string.isRequired,
  categories: PropTypes.arrayOf(PropTypes.object).isRequired,
  protocol: PropTypes.string.isRequired,
  age: PropTypes.number.isRequired,
  ageHours: PropTypes.number.isRequired,
  ageMinutes: PropTypes.number.isRequired,
  publishDate: PropTypes.string.isRequired,
  title: PropTypes.string.isRequired,
  infoUrl: PropTypes.string.isRequired,
  downloadUrl: PropTypes.string.isRequired,
  indexerId: PropTypes.number.isRequired,
  indexer: PropTypes.string.isRequired,
  size: PropTypes.number.isRequired,
  files: PropTypes.number,
  grabs: PropTypes.number,
  seeders: PropTypes.number,
  leechers: PropTypes.number,
  indexerFlags: PropTypes.arrayOf(PropTypes.string).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default SearchIndexRow;
