import PropTypes from 'prop-types';
import React, { Component } from 'react';
import styles from './HistoryRowParameter.css';

class HistoryRowParameter extends Component {

  //
  // Render

  render() {
    const {
      title,
      value
    } = this.props;

    return (
      <div className={styles.parameter}>
        <div className={styles.info}>
          <span>
            {
              title
            }
          </span>
        </div>

        <div
          className={styles.value}
        >
          {
            value
          }
        </div>
      </div>
    );
  }
}

HistoryRowParameter.propTypes = {
  title: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired
};

export default HistoryRowParameter;
