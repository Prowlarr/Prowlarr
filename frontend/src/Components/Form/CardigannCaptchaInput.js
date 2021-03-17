import classNames from 'classnames';
import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import { icons } from 'Helpers/Props';
import FormInputButton from './FormInputButton';
import TextInput from './TextInput';
import styles from './CaptchaInput.css';

function CardigannCaptchaInput(props) {
  const {
    className,
    name,
    value,
    hasError,
    hasWarning,
    type,
    refreshing,
    contentType,
    imageData,
    onChange,
    onRefreshPress
  } = props;

  const img = `data:${contentType};base64,${imageData}`;

  return (
    <div>
      <div className={styles.captchaInputWrapper}>
        <TextInput
          className={classNames(
            className,
            styles.hasButton,
            hasError && styles.hasError,
            hasWarning && styles.hasWarning
          )}
          name={name}
          value={value}
          onChange={onChange}
        />

        <FormInputButton
          onPress={onRefreshPress}
        >
          <Icon
            name={icons.REFRESH}
            isSpinning={refreshing}
          />
        </FormInputButton>
      </div>

      {
        type === 'image' &&
          <div className={styles.recaptchaWrapper}>
            <img
              src={img}
            />
          </div>
      }
    </div>
  );
}

CardigannCaptchaInput.propTypes = {
  className: PropTypes.string.isRequired,
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  hasError: PropTypes.bool,
  hasWarning: PropTypes.bool,
  type: PropTypes.string,
  refreshing: PropTypes.bool.isRequired,
  contentType: PropTypes.string.isRequired,
  imageData: PropTypes.string,
  onChange: PropTypes.func.isRequired,
  onRefreshPress: PropTypes.func.isRequired,
  onCaptchaChange: PropTypes.func.isRequired
};

CardigannCaptchaInput.defaultProps = {
  className: styles.input,
  value: ''
};

export default CardigannCaptchaInput;
