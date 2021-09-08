import PropTypes from 'prop-types';
import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
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

function movieSelector() {
  return (
    createSelector(
      (_, guid) => guid,
      (state) => state.releases.items,
      (guid, releases) => {
        return releases.find((t) => t.guid === guid);
      }
    )
  );
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

function SearchVirtualRow({
  columns,
  guid,
  longDateFormat,
  onGrabPress: propOnGrabPress,
  timeFormat,
  grabError,
  isGrabbing,
  isGrabbed,
  rowData
}) {
  // Uses memo to ensure that each row gets its own selector
  // As shown here https://react-redux.js.org/api/hooks
  const memoTest = useMemo(movieSelector, []);
  const movie = useSelector((state) => memoTest(state, guid));
  const {
    age,
    ageHours,
    ageMinutes,
    categories,
    downloadUrl,
    files,
    grabs,
    indexer,
    indexerFlags,
    indexerId,
    infoUrl,
    leechers,
    protocol,
    publishDate,
    seeders,
    size,
    title
  } = movie;

  function onGrabPress() {
    propOnGrabPress({ guid, indexerId });
  }

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
                  onPress={onGrabPress}
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

SearchVirtualRow.defaultProps = {
  isGrabbing: false,
  isGrabbed: false,
  grabError: ''
};

SearchVirtualRow.propTypes = {
  columns: PropTypes.array.isRequired,
  guid: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  onGrabPress: PropTypes.func.isRequired,
  timeFormat: PropTypes.string.isRequired,
  grabError: PropTypes.string.isRequired,
  isGrabbing: PropTypes.bool.isRequired,
  isGrabbed: PropTypes.bool.isRequired,
  rowData: PropTypes.object.isRequired
};

export default SearchVirtualRow;
