import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import Popover from 'Components/Tooltip/Popover';
import { icons, kinds, tooltipPositions } from 'Helpers/Props';
import { IndexerStatus } from 'Indexer/Indexer';
import translate from 'Utilities/String/translate';
import DisabledIndexerInfo from './DisabledIndexerInfo';
import styles from './IndexerStatusCell.css';

interface IndexerStatusCellProps {
  className: string;
  enabled: boolean;
  redirect: boolean;
  status: IndexerStatus;
  longDateFormat: string;
  timeFormat: string;
  component?: React.ElementType;
}

function IndexerStatusCell(props: IndexerStatusCellProps) {
  const {
    className,
    enabled,
    redirect,
    status,
    longDateFormat,
    timeFormat,
    component: Component = VirtualTableRowCell,
    ...otherProps
  } = props;

  const enableKind = redirect ? kinds.INFO : kinds.SUCCESS;
  const enableIcon = redirect ? icons.REDIRECT : icons.CHECK;
  const enableTitle = redirect
    ? translate('EnabledRedirected')
    : translate('Enabled');

  return (
    <Component className={className} {...otherProps}>
      {
        <Icon
          className={styles.statusIcon}
          kind={enabled ? enableKind : kinds.DEFAULT}
          name={enabled ? enableIcon : icons.BLOCKLIST}
          title={enabled ? enableTitle : translate('EnabledIndexerIsDisabled')}
        />
      }
      {status ? (
        <Popover
          className={styles.indexerStatusTooltip}
          canFlip={true}
          anchor={
            <Icon
              className={styles.statusIcon}
              kind={kinds.DANGER}
              name={icons.WARNING}
            />
          }
          title={translate('IndexerDisabled')}
          body={
            <div>
              <DisabledIndexerInfo
                mostRecentFailure={status.mostRecentFailure}
                initialFailure={status.initialFailure}
                disabledTill={status.disabledTill}
                longDateFormat={longDateFormat}
                timeFormat={timeFormat}
              />
            </div>
          }
          position={tooltipPositions.BOTTOM}
        />
      ) : null}
    </Component>
  );
}

export default IndexerStatusCell;
