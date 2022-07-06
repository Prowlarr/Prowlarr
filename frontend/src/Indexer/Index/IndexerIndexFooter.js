import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { PureComponent } from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';
import styles from './IndexerIndexFooter.css';

class IndexerIndexFooter extends PureComponent {

  render() {
    const {
      indexers,
      colorImpairedMode
    } = this.props;

    const count = indexers.length;
    let enabled = 0;
    let torrent = 0;

    indexers.forEach((s) => {

      if (s.enable) {
        enabled += 1;
      }

      if (s.protocol === 'torrent') {
        torrent++;
      }
    });

    return (
      <div className={styles.footer}>
        <div>
          <div className={styles.legendItem}>
            <div className={styles.enabled} />
            <div>
              {translate('Enabled')}
            </div>
          </div>

          <div className={styles.legendItem}>
            <div className={styles.redirected} />
            <div>
              {translate('EnabledRedirected')}
            </div>
          </div>

          <div className={styles.legendItem}>
            <div className={styles.disabled} />
            <div>
              {translate('Disabled')}
            </div>
          </div>

          <div className={styles.legendItem}>
            <div className={classNames(
              styles.error,
              colorImpairedMode && 'colorImpaired'
            )}
            />
            <div>
              {translate('Error')}
            </div>
          </div>
        </div>

        <div className={styles.statistics}>
          <DescriptionList>
            <DescriptionListItem
              title={translate('Indexers')}
              data={count}
            />

            <DescriptionListItem
              title={translate('Enabled')}
              data={enabled}
            />
          </DescriptionList>

          <DescriptionList>
            <DescriptionListItem
              title={translate('Torrent')}
              data={torrent}
            />

            <DescriptionListItem
              title={translate('Usenet')}
              data={count - torrent}
            />
          </DescriptionList>
        </div>
      </div>
    );
  }
}

IndexerIndexFooter.propTypes = {
  indexers: PropTypes.arrayOf(PropTypes.object).isRequired,
  colorImpairedMode: PropTypes.bool.isRequired
};

export default IndexerIndexFooter;
