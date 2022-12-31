import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons, kinds } from 'Helpers/Props';
import CategoryLabel from 'Search/Table/CategoryLabel';
import Peers from 'Search/Table/Peers';
import ProtocolLabel from 'Search/Table/ProtocolLabel';
import dimensions from 'Styles/Variables/dimensions';
import formatAge from 'Utilities/Number/formatAge';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './SearchIndexOverview.css';

const columnPadding = parseInt(dimensions.movieIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.movieIndexColumnPaddingSmallScreen);

function getContentHeight(rowHeight, isSmallScreen) {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
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

class SearchIndexOverview extends Component {

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
  };

  //
  // Render

  render() {
    const {
      title,
      infoUrl,
      protocol,
      downloadUrl,
      categories,
      seeders,
      leechers,
      size,
      age,
      ageHours,
      ageMinutes,
      indexer,
      rowHeight,
      isSmallScreen,
      isGrabbed,
      isGrabbing,
      grabError
    } = this.props;

    const contentHeight = getContentHeight(rowHeight, isSmallScreen);

    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <div className={styles.info} style={{ height: contentHeight }}>
            <div className={styles.titleRow}>
              <div className={styles.title}>
                <Link
                  to={infoUrl}
                  title={title}
                >
                  <TextTruncate
                    line={2}
                    text={title}
                  />
                </Link>

              </div>

              <div className={styles.actions}>
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
                  title={translate('Save')}
                  to={downloadUrl}
                />
              </div>
            </div>
            <div className={styles.indexerRow}>
              {indexer}
            </div>
            <div className={styles.infoRow}>
              <ProtocolLabel
                protocol={protocol}
              />

              {
                protocol === 'torrent' &&
                  <Peers
                    seeders={seeders}
                    leechers={leechers}
                  />
              }

              <Label>
                {formatBytes(size)}
              </Label>

              <Label>
                {formatAge(age, ageHours, ageMinutes)}
              </Label>

              <CategoryLabel
                categories={categories}
              />
            </div>
          </div>
        </div>
      </div>
    );
  }
}

SearchIndexOverview.propTypes = {
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
  rowHeight: PropTypes.number.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onGrabPress: PropTypes.func.isRequired,
  isGrabbing: PropTypes.bool.isRequired,
  isGrabbed: PropTypes.bool.isRequired,
  grabError: PropTypes.string
};

SearchIndexOverview.defaultProps = {
  isGrabbing: false,
  isGrabbed: false
};

export default SearchIndexOverview;
