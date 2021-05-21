import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatAge from 'Utilities/Number/formatAge';
import formatBytes from 'Utilities/Number/formatBytes';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import CategoryLabel from './CategoryLabel';
import Peers from './Peers';
import ProtocolLabel from './ProtocolLabel';
import styles from './SearchIndexRow.css';

function getDownloadIcon(isGrabbing, isGrabbed, grabError) {
  if (isGrabbing) {
    return icons.SPINNER;
  } else if (isGrabbed) {
    return icons.DOWNLOADING;
  } else if (grabError) {
    return icons.DOWNLOADING;
  }

  return icons.DOWNLOAD;
}

function getDownloadTooltip(isGrabbing, isGrabbed, grabError) {
  if (isGrabbing) {
    return '';
  } else if (isGrabbed) {
    return translate('AddedToDownloadClient');
  } else if (grabError) {
    return grabError;
  }

  return translate('AddToDownloadClient');
}

class SearchIndexRow extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      isConfirmGrabModalOpen: false
    };
  }

  //
  // Listeners

  onGrabPress = () => {
    const {
      guid,
      indexerId,
      onGrabPress
    } = this.props;

    onGrabPress({
      guid,
      indexerId
    });
  }

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
      isGrabbing,
      isGrabbed,
      grabError,
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
                                    {titleCase(flag)}
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
                  <SpinnerIconButton
                    name={getDownloadIcon(isGrabbing, isGrabbed, grabError)}
                    kind={grabError ? kinds.DANGER : kinds.DEFAULT}
                    title={getDownloadTooltip(isGrabbing, isGrabbed, grabError)}
                    isDisabled={isGrabbed}
                    isSpinning={isGrabbing}
                    onPress={this.onGrabPress}
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
  onGrabPress: PropTypes.func.isRequired,
  isGrabbing: PropTypes.bool.isRequired,
  isGrabbed: PropTypes.bool.isRequired,
  grabError: PropTypes.string,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired
};

SearchIndexRow.defaultProps = {
  isGrabbing: false,
  isGrabbed: false
};

export default SearchIndexRow;
