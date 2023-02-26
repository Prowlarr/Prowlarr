import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import DescriptionListItemDescription from 'Components/DescriptionList/DescriptionListItemDescription';
import DescriptionListItemTitle from 'Components/DescriptionList/DescriptionListItemTitle';
import FieldSet from 'Components/FieldSet';
import Label from 'Components/Label';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import Indexer from 'Indexer/Indexer';
import createIndexerSelector from 'Store/Selectors/createIndexerSelector';
import translate from 'Utilities/String/translate';
import styles from './IndexerInfoModalContent.css';

function createIndexerInfoItemSelector(indexerId: number) {
  return createSelector(
    createIndexerSelector(indexerId),
    (indexer: Indexer) => {
      return {
        indexer,
      };
    }
  );
}

interface IndexerInfoModalContentProps {
  indexerId: number;
  onModalClose(): void;
}

function IndexerInfoModalContent(props: IndexerInfoModalContentProps) {
  const { indexer } = useSelector(
    createIndexerInfoItemSelector(props.indexerId)
  );

  const {
    id,
    name,
    description,
    encoding,
    language,
    indexerUrls,
    fields,
    protocol,
    capabilities,
  } = indexer;

  const { onModalClose } = props;

  const baseUrl =
    fields.find((field) => field.name === 'baseUrl')?.value ??
    (Array.isArray(indexerUrls) ? indexerUrls[0] : undefined);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{`${name}`}</ModalHeader>

      <ModalBody>
        <FieldSet legend={translate('IndexerDetails')}>
          <div>
            <DescriptionList>
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('Id')}
                data={id}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('Description')}
                data={description ? description : '-'}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('Encoding')}
                data={encoding ? encoding : '-'}
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('Language')}
                data={language ?? '-'}
              />
              <DescriptionListItemTitle>
                {translate('IndexerSite')}
              </DescriptionListItemTitle>
              <DescriptionListItemDescription>
                <Link to={baseUrl}>{baseUrl.replace('api.', '')}</Link>
              </DescriptionListItemDescription>
              <DescriptionListItemTitle>{`${
                protocol === 'usenet' ? 'Newznab' : 'Torznab'
              } Url`}</DescriptionListItemTitle>
              <DescriptionListItemDescription>
                {`${window.location.origin}${window.Prowlarr.urlBase}/${id}/api`}
              </DescriptionListItemDescription>
            </DescriptionList>
          </div>
        </FieldSet>
        <FieldSet legend={translate('SearchCapabilities')}>
          <div>
            <DescriptionList>
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('RawSearchSupported')}
                data={
                  capabilities.supportsRawSearch
                    ? translate('Yes')
                    : translate('No')
                }
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('SearchTypes')}
                data={
                  capabilities.searchParams.length === 0 ? (
                    translate('NotSupported')
                  ) : (
                    <Label kind={kinds.PRIMARY}>
                      {capabilities.searchParams[0]}
                    </Label>
                  )
                }
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('TVSearchTypes')}
                data={
                  capabilities.tvSearchParams.length === 0
                    ? translate('NotSupported')
                    : capabilities.tvSearchParams.map((p) => {
                        return (
                          <Label key={p} kind={kinds.PRIMARY}>
                            {p}
                          </Label>
                        );
                      })
                }
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('MovieSearchTypes')}
                data={
                  capabilities.movieSearchParams.length === 0
                    ? translate('NotSupported')
                    : capabilities.movieSearchParams.map((p) => {
                        return (
                          <Label key={p} kind={kinds.PRIMARY}>
                            {p}
                          </Label>
                        );
                      })
                }
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('BookSearchTypes')}
                data={
                  capabilities.bookSearchParams.length === 0
                    ? translate('NotSupported')
                    : capabilities.bookSearchParams.map((p) => {
                        return (
                          <Label key={p} kind={kinds.PRIMARY}>
                            {p}
                          </Label>
                        );
                      })
                }
              />
              <DescriptionListItem
                descriptionClassName={styles.description}
                title={translate('MusicSearchTypes')}
                data={
                  capabilities.musicSearchParams.length === 0
                    ? translate('NotSupported')
                    : capabilities.musicSearchParams.map((p) => {
                        return (
                          <Label key={p} kind={kinds.PRIMARY}>
                            {p}
                          </Label>
                        );
                      })
                }
              />
            </DescriptionList>
          </div>
        </FieldSet>
      </ModalBody>
      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default IndexerInfoModalContent;
