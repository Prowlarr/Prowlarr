import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Alert from 'Components/Alert';
import { kinds } from 'Helpers/Props';
import styles from './InfoInput.css';

class InfoInput extends Component {

  //
  // Render

  render() {
    const { value } = this.props;

    return (
      <Alert
        kind={kinds.INFO}
        className={styles.message}
      >
        <span dangerouslySetInnerHTML={{ __html: value }} />
      </Alert>
    );
  }
}

InfoInput.propTypes = {
  readOnly: PropTypes.bool,
  autoFocus: PropTypes.bool,
  placeholder: PropTypes.string,
  name: PropTypes.string.isRequired,
  value: PropTypes.oneOfType([PropTypes.string, PropTypes.number, PropTypes.array]).isRequired,
  hasError: PropTypes.bool,
  hasWarning: PropTypes.bool,
  hasButton: PropTypes.bool,
  onChange: PropTypes.func.isRequired,
  onFocus: PropTypes.func,
  onBlur: PropTypes.func,
  onCopy: PropTypes.func,
  onSelectionChange: PropTypes.func
};

InfoInput.defaultProps = {
  value: ''
};

export default InfoInput;
