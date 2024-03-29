import classNames from 'classnames';
import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import styles from './Message.css';

function getIconName(name) {
  switch (name) {
    case 'ApplicationUpdate':
      return icons.RESTART;
    case 'Backup':
      return icons.BACKUP;
    case 'CheckHealth':
      return icons.HEALTH;
    case 'Housekeeping':
      return icons.HOUSEKEEPING;
    default:
      return icons.SPINNER;
  }
}

function Message(props) {
  const {
    name,
    message,
    type
  } = props;

  return (
    <div className={classNames(
      styles.message,
      styles[type]
    )}
    >
      <div className={styles.iconContainer}>
        <Icon
          name={getIconName(name)}
          title={name}
        />
      </div>

      <div
        className={styles.text}
      >
        {message}
      </div>
    </div>
  );
}

Message.propTypes = {
  name: PropTypes.string.isRequired,
  message: PropTypes.string.isRequired,
  type: PropTypes.string.isRequired
};

export default Message;
