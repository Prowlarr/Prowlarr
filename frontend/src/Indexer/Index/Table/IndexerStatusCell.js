import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import styles from './IndexerStatusCell.css';

function IndexerStatusCell(props) {
  const {
    className,
    enabled,
    component: Component,
    ...otherProps
  } = props;

  return (
    <Component
      className={className}
      {...otherProps}
    >
      {
        !enabled &&
          <Icon
            className={styles.statusIcon}
            name={icons.BLACKLIST}
            title={enabled ? 'Indexer is Enabled' : 'Indexer is Disabled'}
          />
      }
    </Component>
  );
}

IndexerStatusCell.propTypes = {
  className: PropTypes.string.isRequired,
  enabled: PropTypes.bool.isRequired,
  component: PropTypes.elementType
};

IndexerStatusCell.defaultProps = {
  className: styles.status,
  component: VirtualTableRowCell
};

export default IndexerStatusCell;
