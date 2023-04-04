import classNames from 'classnames';
import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { ColorImpairedConsumer } from 'App/ColorImpairedContext';
import IndexerAppState from 'App/State/IndexerAppState';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createDeepEqualSelector from 'Store/Selectors/createDeepEqualSelector';
import translate from 'Utilities/String/translate';
import styles from './IndexerIndexFooter.css';

function createUnoptimizedSelector() {
  return createSelector(
    createClientSideCollectionSelector('indexers', 'indexerIndex'),
    (indexers: IndexerAppState) => {
      return indexers.items.map((s) => {
        const { protocol, privacy, enable } = s;

        return {
          protocol,
          privacy,
          enable,
        };
      });
    }
  );
}

function createIndexersSelector() {
  return createDeepEqualSelector(
    createUnoptimizedSelector(),
    (indexers) => indexers
  );
}

export default function IndexerIndexFooter() {
  const indexers = useSelector(createIndexersSelector());
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
    <ColorImpairedConsumer>
      {(enableColorImpairedMode) => {
        return (
          <div className={styles.footer}>
            <div>
              <div className={styles.legendItem}>
                <div className={styles.enabled} />
                <div>{translate('Enabled')}</div>
              </div>

              <div className={styles.legendItem}>
                <div className={styles.redirected} />
                <div>{translate('EnabledRedirected')}</div>
              </div>

              <div className={styles.legendItem}>
                <div className={styles.disabled} />
                <div>{translate('Disabled')}</div>
              </div>

              <div className={styles.legendItem}>
                <div
                  className={classNames(
                    styles.error,
                    enableColorImpairedMode && 'colorImpaired'
                  )}
                />
                <div>{translate('Error')}</div>
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
      }}
    </ColorImpairedConsumer>
  );
}
