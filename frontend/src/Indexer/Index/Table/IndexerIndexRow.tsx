import React, { useCallback, useState } from 'react';
import { useSelector } from 'react-redux';
import { useSelect } from 'App/SelectContext';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import RelativeDateCellConnector from 'Components/Table/Cells/RelativeDateCellConnector';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import Column from 'Components/Table/Column';
import TagListConnector from 'Components/TagListConnector';
import { icons } from 'Helpers/Props';
import DeleteIndexerModal from 'Indexer/Delete/DeleteIndexerModal';
import EditIndexerModalConnector from 'Indexer/Edit/EditIndexerModalConnector';
import createIndexerIndexItemSelector from 'Indexer/Index/createIndexerIndexItemSelector';
import IndexerTitleLink from 'Indexer/IndexerTitleLink';
import firstCharToUpper from 'Utilities/String/firstCharToUpper';
import translate from 'Utilities/String/translate';
import CapabilitiesLabel from './CapabilitiesLabel';
import IndexerStatusCell from './IndexerStatusCell';
import ProtocolLabel from './ProtocolLabel';
import styles from './IndexerIndexRow.css';

interface IndexerIndexRowProps {
  indexerId: number;
  sortKey: string;
  columns: Column[];
  isSelectMode: boolean;
}

function IndexerIndexRow(props: IndexerIndexRowProps) {
  const { indexerId, columns, isSelectMode } = props;

  const { indexer, appProfile, status, longDateFormat, timeFormat } =
    useSelector(createIndexerIndexItemSelector(props.indexerId));

  const {
    id,
    name: indexerName,
    indexerUrls,
    enable,
    redirect,
    tags,
    protocol,
    privacy,
    priority,
    fields,
    added,
    capabilities,
  } = indexer;

  const baseUrl =
    fields.find((field) => field.name === 'baseUrl')?.value ??
    (Array.isArray(indexerUrls) ? indexerUrls[0] : undefined);

  const vipExpiration =
    fields.find((field) => field.name === 'vipExpiration')?.value ?? '';

  const rssUrl = `${window.location.origin}${
    window.Prowlarr.urlBase
  }/${id}/api?apikey=${encodeURIComponent(
    window.Prowlarr.apiKey
  )}&extended=1&t=search`;

  const [isEditIndexerModalOpen, setIsEditIndexerModalOpen] = useState(false);
  const [isDeleteIndexerModalOpen, setIsDeleteIndexerModalOpen] =
    useState(false);
  const [selectState, selectDispatch] = useSelect();

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
  }, [setIsDeleteIndexerModalOpen]);

  const checkInputCallback = useCallback(() => {
    // Mock handler to satisfy `onChange` being required for `CheckInput`.
  }, []);

  const onSelectedChange = useCallback(
    ({ id, value, shiftKey }) => {
      selectDispatch({
        type: 'toggleSelected',
        id,
        isSelected: value,
        shiftKey,
      });
    },
    [selectDispatch]
  );

  return (
    <>
      {isSelectMode ? (
        <VirtualTableSelectCell
          id={indexerId}
          isSelected={selectState.selectedState[indexerId]}
          isDisabled={false}
          onSelectedChange={onSelectedChange}
        />
      ) : null}

      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'status') {
          return (
            <IndexerStatusCell
              key={name}
              className={styles[name]}
              enabled={enable}
              redirect={redirect}
              status={status}
              longDateFormat={longDateFormat}
              timeFormat={timeFormat}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'sortName') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <IndexerTitleLink
                indexerId={indexerId}
                indexerName={indexerName}
              />
            </VirtualTableRowCell>
          );
        }

        if (name === 'privacy') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <Label>{translate(firstCharToUpper(privacy))}</Label>
            </VirtualTableRowCell>
          );
        }

        if (name === 'priority') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {priority}
            </VirtualTableRowCell>
          );
        }

        if (name === 'protocol') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <ProtocolLabel protocol={protocol} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'appProfileId') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {appProfile?.name || ''}
            </VirtualTableRowCell>
          );
        }

        if (name === 'capabilities') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <CapabilitiesLabel capabilities={capabilities} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'added') {
          return (
            <RelativeDateCellConnector
              key={name}
              className={styles[name]}
              date={added.toString()}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'vipExpiration') {
          return (
            <RelativeDateCellConnector
              key={name}
              className={styles[name]}
              date={vipExpiration}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'tags') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <TagListConnector tags={tags} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <VirtualTableRowCell
              key={column.name}
              className={styles[column.name]}
            >
              <IconButton
                className={styles.externalLink}
                name={icons.RSS}
                title={translate('RssFeed')}
                to={rssUrl}
              />

              {baseUrl ? (
                <IconButton
                  className={styles.externalLink}
                  name={icons.EXTERNAL_LINK}
                  title={translate('Website')}
                  to={baseUrl.replace(/(:\/\/)api\./, '$1')}
                />
              ) : null}

              <IconButton
                name={icons.EDIT}
                title={translate('EditIndexer')}
                onPress={onEditIndexerPress}
              />
            </VirtualTableRowCell>
          );
        }

        return null;
      })}

      <EditIndexerModalConnector
        isOpen={isEditIndexerModalOpen}
        id={indexerId}
        onModalClose={onEditIndexerModalClose}
        onDeleteIndexerPress={onDeleteIndexerPress}
      />

      <DeleteIndexerModal
        isOpen={isDeleteIndexerModalOpen}
        indexerId={indexerId}
        onModalClose={onDeleteIndexerModalClose}
      />
    </>
  );
}

export default IndexerIndexRow;
