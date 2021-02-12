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
    status,
    longDateFormat,
    timeFormat,
    component: Component,
    ...otherProps
  } = props;

  return (
    <Component
      className={className}
      {...otherProps}
    >
      {
        <Icon
          className={styles.statusIcon}
          kind={enabled ? kinds.SUCCESS : kinds.DEFAULT}
          name={enabled ? icons.CHECK : icons.BLACKLIST}
          title={enabled ? 'Indexer is Enabled' : 'Indexer is Disabled'}
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
