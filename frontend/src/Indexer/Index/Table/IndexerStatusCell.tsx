import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import { IndexerStatus } from 'Indexer/Indexer';
import formatDateTime from 'Utilities/Date/formatDateTime';
import styles from './IndexerStatusCell.css';

interface IndexerStatusCellProps {
  className: string;
  enabled: boolean;
  redirect: boolean;
  status: IndexerStatus;
  component?: React.ElementType;
}

function IndexerStatusCell(props: IndexerStatusCellProps) {
  const {
    className,
    enabled,
    redirect,
    status,
    component: Component = VirtualTableRowCell,
    ...otherProps
  } = props;

  const enableKind = redirect ? kinds.INFO : kinds.SUCCESS;
  const enableIcon = redirect ? icons.REDIRECT : icons.CHECK;
  const enableTitle = redirect
    ? 'Indexer is Enabled, Redirect is Enabled'
    : 'Indexer is Enabled';

  return (
    <Component className={className} {...otherProps}>
      {
        <Icon
          className={styles.statusIcon}
          kind={enabled ? enableKind : kinds.DEFAULT}
          name={enabled ? enableIcon : icons.BLOCKLIST}
          title={enabled ? enableTitle : 'Indexer is Disabled'}
        />
      }
      {status ? (
        <Icon
          className={styles.statusIcon}
          kind={kinds.DANGER}
          name={icons.WARNING}
          title={`Indexer is Disabled due to failures until ${formatDateTime(
            status.disabledTill
          )}`}
        />
      ) : null}
    </Component>
  );
}

export default IndexerStatusCell;
