import React, { useCallback } from 'react';
import Label from 'Components/Label';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import { kinds } from 'Helpers/Props';
import { ApplicationSyncLevel } from 'typings/Application';
import { SelectStateInputProps } from 'typings/props';
import translate from 'Utilities/String/translate';
import styles from './ManageApplicationsModalRow.css';

interface ManageApplicationsModalRowProps {
  id: number;
  name: string;
  syncLevel: string;
  implementation: string;
  tags: number[];
  columns: Column[];
  isSelected?: boolean;
  onSelectedChange(result: SelectStateInputProps): void;
}

function ManageApplicationsModalRow(props: ManageApplicationsModalRowProps) {
  const {
    id,
    isSelected,
    name,
    syncLevel,
    implementation,
    tags,
    onSelectedChange,
  } = props;

  const onSelectedChangeWrapper = useCallback(
    (result: SelectStateInputProps) => {
      onSelectedChange({
        ...result,
      });
    },
    [onSelectedChange]
  );

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChangeWrapper}
      />

      <TableRowCell className={styles.name}>{name}</TableRowCell>

      <TableRowCell className={styles.implementation}>
        {implementation}
      </TableRowCell>

      <TableRowCell className={styles.syncLevel}>
        {syncLevel === ApplicationSyncLevel.AddOnly && (
          <Label kind={kinds.WARNING}>{translate('AddRemoveOnly')}</Label>
        )}

        {syncLevel === ApplicationSyncLevel.FullSync && (
          <Label kind={kinds.SUCCESS}>{translate('FullSync')}</Label>
        )}

        {syncLevel === ApplicationSyncLevel.Disabled && (
          <Label kind={kinds.DISABLED} outline={true}>
            {translate('Disabled')}
          </Label>
        )}
      </TableRowCell>

      <TableRowCell className={styles.tags}>
        <TagListConnector tags={tags} />
      </TableRowCell>
    </TableRow>
  );
}

export default ManageApplicationsModalRow;
