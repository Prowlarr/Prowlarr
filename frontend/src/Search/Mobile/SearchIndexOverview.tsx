import React, { useCallback, useMemo, useState } from 'react';
import { useSelector } from 'react-redux';
import TextTruncate from 'react-text-truncate';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import { icons, kinds } from 'Helpers/Props';
import ProtocolLabel from 'Indexer/Index/Table/ProtocolLabel';
import { IndexerCategory } from 'Indexer/Indexer';
import OverrideMatchModal from 'Search/OverrideMatch/OverrideMatchModal';
import CategoryLabel from 'Search/Table/CategoryLabel';
import Peers from 'Search/Table/Peers';
import createEnabledDownloadClientsSelector from 'Store/Selectors/createEnabledDownloadClientsSelector';
import dimensions from 'Styles/Variables/dimensions';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatAge from 'Utilities/Number/formatAge';
import formatBytes from 'Utilities/Number/formatBytes';
import titleCase from 'Utilities/String/titleCase';
import translate from 'Utilities/String/translate';
import styles from './SearchIndexOverview.css';

const columnPadding = parseInt(dimensions.movieIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.movieIndexColumnPaddingSmallScreen
);

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

interface SearchIndexOverviewProps {
  guid: string;
  protocol: DownloadProtocol;
  age: number;
  ageHours: number;
  ageMinutes: number;
  publishDate: string;
  title: string;
  infoUrl: string;
  downloadUrl?: string;
  magnetUrl?: string;
  indexerId: number;
  indexer: string;
  categories: IndexerCategory[];
  size: number;
  seeders?: number;
  leechers?: number;
  indexerFlags: string[];
  isGrabbing: boolean;
  isGrabbed: boolean;
  grabError?: string;
  longDateFormat: string;
  timeFormat: string;
  rowHeight: number;
  isSmallScreen: boolean;
  onGrabPress(...args: unknown[]): void;
}

function SearchIndexOverview(props: SearchIndexOverviewProps) {
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
    infoUrl,
    downloadUrl,
    magnetUrl,
    indexer,
    size,
    seeders,
    leechers,
    indexerFlags = [],
    isGrabbing = false,
    isGrabbed = false,
    grabError,
    longDateFormat,
    timeFormat,
    rowHeight,
    isSmallScreen,
    onGrabPress,
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

  const onOverridePress = useCallback(() => {
    setIsOverrideModalOpen(true);
  }, [setIsOverrideModalOpen]);

  const onOverrideModalClose = useCallback(() => {
    setIsOverrideModalOpen(false);
  }, [setIsOverrideModalOpen]);

  const contentHeight = useMemo(() => {
    const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

    return rowHeight - padding;
  }, [rowHeight, isSmallScreen]);

  return (
    <>
      <div className={styles.container}>
        <div className={styles.content}>
          <div className={styles.info} style={{ height: contentHeight }}>
            <div className={styles.titleRow}>
              <div className={styles.title}>
                <Link to={infoUrl} title={title}>
                  <TextTruncate line={2} text={title} />
                </Link>
              </div>

              <div className={styles.actions}>
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
                        size={11}
                      />

                      <Icon
                        className={styles.downloadIcon}
                        name={icons.CIRCLE_DOWN}
                        size={9}
                      />
                    </div>
                  </Link>
                ) : null}

                {downloadUrl || magnetUrl ? (
                  <IconButton
                    className={styles.downloadLink}
                    name={icons.SAVE}
                    title={translate('Save')}
                    to={downloadUrl ?? magnetUrl}
                  />
                ) : null}
              </div>
            </div>
            <div className={styles.indexerRow}>{indexer}</div>
            <div className={styles.infoRow}>
              <ProtocolLabel protocol={protocol} />

              {protocol === 'torrent' && (
                <Peers seeders={seeders} leechers={leechers} />
              )}

              <Label>{formatBytes(size)}</Label>

              <Label
                title={formatDateTime(publishDate, longDateFormat, timeFormat, {
                  includeSeconds: true,
                })}
              >
                {formatAge(age, ageHours, ageMinutes)}
              </Label>

              <CategoryLabel categories={categories} />

              {indexerFlags.length
                ? indexerFlags
                    .sort((a, b) => a.localeCompare(b))
                    .map((flag, index) => {
                      return (
                        <Label key={index} kind={kinds.INFO}>
                          {titleCase(flag)}
                        </Label>
                      );
                    })
                : null}
            </div>
          </div>
        </div>
      </div>

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

export default SearchIndexOverview;
