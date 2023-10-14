import React, { SyntheticEvent, useCallback, useState } from 'react';
import IconButton from 'Components/Link/IconButton';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import TextInput from './TextInput';
import styles from './PrivacyTextInput.css';

interface PrivacyTextInputProps {
  className: string;
}

function PrivacyTextInput(props: PrivacyTextInputProps) {
  const { className = styles.input, ...otherProps } = props;

  const [isVisible, setIsVisible] = useState(false);

  const toggleVisibility = useCallback(() => {
    setIsVisible(!isVisible);
  }, [isVisible, setIsVisible]);

  // Prevent a user from copying (or cutting) the password from the input
  const onCopy = useCallback((event: SyntheticEvent) => {
    event.preventDefault();
    event.nativeEvent.stopImmediatePropagation();
  }, []);

  return (
    <div className={styles.container}>
      <TextInput
        className={className}
        {...otherProps}
        onCopy={onCopy}
        type={isVisible ? 'text' : 'password'}
      />

      <IconButton
        className={styles.toggle}
        name={isVisible ? icons.VIEW : icons.HIDE}
        title={translate('Toggle')}
        onPress={toggleVisibility}
      />
    </div>
  );
}

export default PrivacyTextInput;
