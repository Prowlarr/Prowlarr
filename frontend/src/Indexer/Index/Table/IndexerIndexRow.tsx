import React, { useCallback, useState } from 'react';
import { useSelector } from 'react-redux';
import { SelectActionType, useSelect } from 'App/SelectContext';
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
import IndexerInfoModal from 'Indexer/Info/IndexerInfoModal';
import titleCase from 'Utilities/String/titleCase';
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

  const { indexer, appProfile } = useSelector(
    createIndexerIndexItemSelector(props.indexerId)
  );

  const {
    name: indexerName,
    indexerUrls,
    enable,
    redirect,
    tags,
    protocol,
    privacy,
    priority,
    status,
    fields,
    added,
    capabilities,
  } = indexer;

  const baseUrl =
    fields.find((field) => field.name === 'baseUrl')?.value ??
    (Array.isArray(indexerUrls) ? indexerUrls[0] : undefined);

  const [isIndexerInfoModalOpen, setIsIndexerInfoModalOpen] = useState(false);
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

  const onIndexerInfoPress = useCallback(() => {
    setIsIndexerInfoModalOpen(true);
  }, [setIsIndexerInfoModalOpen]);

  const onIndexerInfoModalClose = useCallback(() => {
    setIsIndexerInfoModalOpen(false);
  }, [setIsIndexerInfoModalOpen]);

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
        type: SelectActionType.ToggleSelected,
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
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'sortName') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {indexerName}
            </VirtualTableRowCell>
          );
        }

        if (name === 'privacy') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <Label>{titleCase(privacy)}</Label>
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
                name={icons.INFO}
                title={translate('IndexerInfo')}
                onPress={onIndexerInfoPress}
              />

              {baseUrl ? (
                <IconButton
                  className={styles.externalLink}
                  name={icons.EXTERNAL_LINK}
                  title={translate('Website')}
                  to={baseUrl.replace('api.', '')}
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

      <IndexerInfoModal
        indexerId={indexerId}
        isOpen={isIndexerInfoModalOpen}
        onModalClose={onIndexerInfoModalClose}
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
