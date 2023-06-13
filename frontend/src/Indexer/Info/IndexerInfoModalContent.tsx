import React, { useCallback, useState } from 'react';
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
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import { kinds } from 'Helpers/Props';
import DeleteIndexerModal from 'Indexer/Delete/DeleteIndexerModal';
import EditIndexerModalConnector from 'Indexer/Edit/EditIndexerModalConnector';
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
    tags,
    protocol,
    capabilities,
  } = indexer;

  const { onModalClose } = props;

  const baseUrl =
    fields.find((field) => field.name === 'baseUrl')?.value ??
    (Array.isArray(indexerUrls) ? indexerUrls[0] : undefined);

  const vipExpiration =
    fields.find((field) => field.name === 'vipExpiration')?.value ?? undefined;

  const [isEditIndexerModalOpen, setIsEditIndexerModalOpen] = useState(false);
  const [isDeleteIndexerModalOpen, setIsDeleteIndexerModalOpen] =
    useState(false);

  const onEditIndexerPress = useCallback(() => {
    setIsEditIndexerModalOpen(true);
  }, [setIsEditIndexerModalOpen]);

  const onEditIndexerModalClose = useCallback(() => {
    setIsEditIndexerModalOpen(false);
  }, [setIsEditIndexerModalOpen]);

  const onDeleteIndexerPress = useCallback(() => {
    setIsEditIndexerModalOpen(false);
    setIsDeleteIndexerModalOpen(true);
  }, [setIsDeleteIndexerModalOpen]);

  const onDeleteIndexerModalClose = useCallback(() => {
    setIsDeleteIndexerModalOpen(false);
    onModalClose();
  }, [setIsDeleteIndexerModalOpen, onModalClose]);

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
              {vipExpiration ? (
                <DescriptionListItem
                  descriptionClassName={styles.description}
                  title={translate('VipExpiration')}
                  data={vipExpiration}
                />
              ) : null}
              <DescriptionListItemTitle>
                {translate('IndexerSite')}
              </DescriptionListItemTitle>
              <DescriptionListItemDescription>
                <Link to={baseUrl}>
                  {baseUrl.replace(/(:\/\/)api\./, '$1')}
                </Link>
              </DescriptionListItemDescription>
              <DescriptionListItemTitle>{`${
                protocol === 'usenet' ? 'Newznab' : 'Torznab'
              } Url`}</DescriptionListItemTitle>
              <DescriptionListItemDescription>
                {`${window.location.origin}${window.Prowlarr.urlBase}/${id}/api`}
              </DescriptionListItemDescription>
              {tags.length > 0 ? (
                <>
                  <DescriptionListItemTitle>
                    {translate('Tags')}
                  </DescriptionListItemTitle>
                  <DescriptionListItemDescription>
                    <TagListConnector tags={tags} />
                  </DescriptionListItemDescription>
                </>
              ) : null}
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

        {capabilities.categories !== null &&
        capabilities.categories.length > 0 ? (
          <FieldSet legend={translate('IndexerCategories')}>
            <Table
              columns={[
                {
                  name: 'id',
                  label: translate('Id'),
                  isVisible: true,
                },
                {
                  name: 'name',
                  label: translate('Name'),
                  isVisible: true,
                },
              ]}
            >
              {capabilities.categories
                .sort((a, b) => a.id - b.id)
                .map((category) => {
                  return (
                    <TableBody key={category.id}>
                      <TableRow key={category.id}>
                        <TableRowCell>{category.id}</TableRowCell>
                        <TableRowCell>{category.name}</TableRowCell>
                      </TableRow>
                      {category.subCategories !== null &&
                      category.subCategories.length > 0
                        ? category.subCategories
                            .sort((a, b) => a.id - b.id)
                            .map((subCategory) => {
                              return (
                                <TableRow key={subCategory.id}>
                                  <TableRowCell>{subCategory.id}</TableRowCell>
                                  <TableRowCell>
                                    {subCategory.name}
                                  </TableRowCell>
                                </TableRow>
                              );
                            })
                        : null}
                    </TableBody>
                  );
                })}
            </Table>
          </FieldSet>
        ) : null}
      </ModalBody>
      <ModalFooter>
        <Button
          className={styles.deleteButton}
          kind={kinds.DANGER}
          onPress={onDeleteIndexerPress}
        >
          {translate('Delete')}
        </Button>
        <Button onPress={onEditIndexerPress}>{translate('Edit')}</Button>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>

      <EditIndexerModalConnector
        isOpen={isEditIndexerModalOpen}
        id={id}
        onModalClose={onEditIndexerModalClose}
        onDeleteIndexerPress={onDeleteIndexerPress}
      />

      <DeleteIndexerModal
        isOpen={isDeleteIndexerModalOpen}
        indexerId={id}
        onModalClose={onDeleteIndexerModalClose}
      />
    </ModalContent>
  );
}

export default IndexerInfoModalContent;
