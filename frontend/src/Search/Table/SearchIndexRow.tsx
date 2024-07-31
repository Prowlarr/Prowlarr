import React, { useCallback, useState } from 'react';
import { useSelector } from 'react-redux';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import Column from 'Components/Table/Column';
import Popover from 'Components/Tooltip/Popover';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';
import { IndexerCategory } from 'Indexer/Indexer';
import OverrideMatchModal from 'Search/OverrideMatch/OverrideMatchModal';
import createEnabledDownloadClientsSelector from 'Store/Selectors/createEnabledDownloadClientsSelector';
import { SelectStateInputProps } from 'typings/props';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatAge from 'Utilities/Number/formatAge';
import formatBytes from 'Utilities/Number/formatBytes';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import CategoryLabel from './CategoryLabel';
import Peers from './Peers';
import ReleaseLinks from './ReleaseLinks';
import styles from './SearchIndexRow.css';

function getDownloadIcon(
  isGrabbing: boolean,
  isGrabbed: boolean,
  grabError?: string
) {
  if (isGrabbing) {
    return icons.SPINNER;
  } else if (isGrabbed) {
    return icons.DOWNLOADING;
  } else if (grabError) {
    return icons.DOWNLOADING;
  }

  return icons.DOWNLOAD;
}

function getDownloadKind(isGrabbed: boolean, grabError?: string) {
  if (isGrabbed) {
    return kinds.SUCCESS;
  }

  if (grabError) {
    return kinds.DANGER;
  }

  return kinds.DEFAULT;
}

function getDownloadTooltip(
  isGrabbing: boolean,
  isGrabbed: boolean,
  grabError?: string
) {
  if (isGrabbing) {
    return '';
  } else if (isGrabbed) {
    return translate('AddedToDownloadClient');
  } else if (grabError) {
    return grabError;
  }

  return translate('AddToDownloadClient');
}

interface SearchIndexRowProps {
  guid: string;
  protocol: DownloadProtocol;
  age: number;
  ageHours: number;
  ageMinutes: number;
  publishDate: string;
  title: string;
  fileName: string;
  infoUrl: string;
  downloadUrl?: string;
  magnetUrl?: string;
  indexerId: number;
  indexer: string;
  categories: IndexerCategory[];
  size: number;
  files?: number;
  grabs?: number;
  seeders?: number;
  leechers?: number;
  imdbId?: string;
  tmdbId?: number;
  tvdbId?: number;
  tvMazeId?: number;
  indexerFlags: string[];
  isGrabbing: boolean;
  isGrabbed: boolean;
  grabError?: string;
  longDateFormat: string;
  timeFormat: string;
  columns: Column[];
  isSelected?: boolean;
  onSelectedChange(result: SelectStateInputProps): void;
  onGrabPress(...args: unknown[]): void;
  onSavePress(...args: unknown[]): void;
}

function SearchIndexRow(props: SearchIndexRowProps) {
  const {
    guid,
    indexerId,
    protocol,
    categories,
    age,
    ageHours,
    ageMinutes,
    publishDate,
    title,
    fileName,
    infoUrl,
    downloadUrl,
    magnetUrl,
    indexer,
    size,
    files,
    grabs,
    seeders,
    leechers,
    imdbId,
    tmdbId,
    tvdbId,
    tvMazeId,
    indexerFlags = [],
    isGrabbing = false,
    isGrabbed = false,
    grabError,
    longDateFormat,
    timeFormat,
    columns,
    isSelected,
    onSelectedChange,
    onGrabPress,
    onSavePress,
  } = props;

  const [isOverrideModalOpen, setIsOverrideModalOpen] = useState(false);

  const { items: downloadClients } = useSelector(
    createEnabledDownloadClientsSelector(protocol)
  );

  const onGrabPressWrapper = useCallback(() => {
    onGrabPress({
      guid,
      indexerId,
    });
  }, [guid, indexerId, onGrabPress]);

  const onSavePressWrapper = useCallback(() => {
    onSavePress({
      downloadUrl,
      fileName,
    });
  }, [downloadUrl, fileName, onSavePress]);

  const onOverridePress = useCallback(() => {
    setIsOverrideModalOpen(true);
  }, [setIsOverrideModalOpen]);

  const onOverrideModalClose = useCallback(() => {
    setIsOverrideModalOpen(false);
  }, [setIsOverrideModalOpen]);

  return (
    <>
      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'select') {
          return (
            <VirtualTableSelectCell
              key={name}
              inputClassName={styles.checkInput}
              id={guid}
              isSelected={isSelected}
              isDisabled={false}
              onSelectedChange={onSelectedChange}
            />
          );
        }

        if (name === 'protocol') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <ProtocolLabel protocol={protocol} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'age') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name]}
              title={formatDateTime(publishDate, longDateFormat, timeFormat, {
                includeSeconds: true,
              })}
            >
              {formatAge(age, ageHours, ageMinutes)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'sortTitle') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <Link to={infoUrl} title={title}>
                <div>{title}</div>
              </Link>
            </VirtualTableRowCell>
          );
        }

        if (name === 'indexer') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {indexer}
            </VirtualTableRowCell>
          );
        }

        if (name === 'size') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {formatBytes(size)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'files') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {files}
            </VirtualTableRowCell>
          );
        }

        if (name === 'grabs') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {grabs}
            </VirtualTableRowCell>
          );
        }

        if (name === 'peers') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {protocol === 'torrent' && (
                <Peers seeders={seeders} leechers={leechers} />
              )}
            </VirtualTableRowCell>
          );
        }

        if (name === 'category') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <CategoryLabel categories={categories} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'indexerFlags') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {!!indexerFlags.length && (
                <Popover
                  anchor={<Icon name={icons.FLAG} kind={kinds.PRIMARY} />}
                  title={translate('IndexerFlags')}
                  body={
                    <ul>
                      {indexerFlags.map((flag, index) => {
                        return <li key={index}>{titleCase(flag)}</li>;
                      })}
                    </ul>
                  }
                  position={tooltipPositions.LEFT}
                />
              )}
            </VirtualTableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <SpinnerIconButton
                name={getDownloadIcon(isGrabbing, isGrabbed, grabError)}
                kind={getDownloadKind(isGrabbed, grabError)}
                title={getDownloadTooltip(isGrabbing, isGrabbed, grabError)}
                isDisabled={isGrabbed}
                isSpinning={isGrabbing}
                onPress={onGrabPressWrapper}
              />

              {downloadClients.length > 1 ? (
                <Link
                  className={styles.manualDownloadContent}
                  title={translate('OverrideAndAddToDownloadClient')}
                  onPress={onOverridePress}
                >
                  <div className={styles.manualDownloadContent}>
                    <Icon
                      className={styles.interactiveIcon}
                      name={icons.INTERACTIVE}
                      size={12}
                    />

                    <Icon
                      className={styles.downloadIcon}
                      name={icons.CIRCLE_DOWN}
                      size={10}
                    />
                  </div>
                </Link>
              ) : null}

              {downloadUrl ? (
                <IconButton
                  className={styles.downloadLink}
                  name={icons.SAVE}
                  title={translate('Save')}
                  onPress={onSavePressWrapper}
                />
              ) : null}

              {magnetUrl ? (
                <IconButton
                  className={styles.downloadLink}
                  name={icons.MAGNET}
                  title={translate('Open')}
                  to={magnetUrl}
                />
              ) : null}

              {imdbId || tmdbId || tvdbId || tvMazeId ? (
                <Popover
                  anchor={
                    <Icon
                      className={styles.externalLinks}
                      name={icons.EXTERNAL_LINK}
                      size={12}
                    />
                  }
                  title={translate('Links')}
                  body={
                    <ReleaseLinks
                      categories={categories}
                      imdbId={imdbId}
                      tmdbId={tmdbId}
                      tvdbId={tvdbId}
                      tvMazeId={tvMazeId}
                    />
                  }
                  position={tooltipPositions.TOP}
                />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        return null;
      })}

      <OverrideMatchModal
        isOpen={isOverrideModalOpen}
        title={title}
        indexerId={indexerId}
        guid={guid}
        protocol={protocol}
        isGrabbing={isGrabbing}
        grabError={grabError}
        onModalClose={onOverrideModalClose}
      />
    </>
  );
}

export default SearchIndexRow;
