import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons, kinds } from 'Helpers/Props';
import formatDateTime from 'Utilities/Date/formatDateTime';
import styles from './IndexerStatusCell.css';

function IndexerStatusCell(props) {
  const {
    className,
    enabled,
    redirect,
    status,
    longDateFormat,
    timeFormat,
    component: Component,
    ...otherProps
  } = props;

  const enableKind = redirect ? kinds.INFO : kinds.SUCCESS;
  const enableIcon = redirect ? icons.REDIRECT : icons.CHECK;
  const enableTitle = redirect ? 'Indexer is Enabled, Redirect is Enabled' : 'Indexer is Enabled';

  return (
    <Component
      className={className}
      {...otherProps}
    >
      {
        <Icon
          className={styles.statusIcon}
          kind={enabled ? enableKind : kinds.DEFAULT}
          name={enabled ? enableIcon: icons.BLACKLIST}
          title={enabled ? enableTitle : 'Indexer is Disabled'}
        />
      }
      {
        status &&
          <Icon
            className={styles.statusIcon}
            kind={kinds.DANGER}
            name={icons.WARNING}
            title={`Indexer is Disabled due to failures until ${formatDateTime(status.disabledTill, longDateFormat, timeFormat)}`}
          />
      }
    </Component>
  );
}

IndexerStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  enabled: PropTypes.bool.isRequired,
  redirect: PropTypes.bool.isRequired,
  status: PropTypes.object,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  component: PropTypes.elementType
};

IndexerStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default IndexerStatusCell;
