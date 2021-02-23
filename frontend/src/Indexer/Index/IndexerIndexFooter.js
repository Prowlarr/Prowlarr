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
      movies,
      colorImpairedMode
    } = this.props;

    const count = movies.length;
    let enabled = 0;
    let torrent = 0;

    movies.forEach((s) => {

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
            <div className={styles.ended} />
            <div>
              Enabled
            </div>
          </div>

          <div className={styles.legendItem}>
            <div className={styles.availNotMonitored} />
            <div>
              Disabled
            </div>
          </div>

          <div className={styles.legendItem}>
            <div className={classNames(
              styles.missingMonitored,
              colorImpairedMode && 'colorImpaired'
            )}
            />
            <div>
              Error
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
  movies: PropTypes.arrayOf(PropTypes.object).isRequired,
  colorImpairedMode: PropTypes.bool.isRequired
};

export default IndexerIndexFooter;
