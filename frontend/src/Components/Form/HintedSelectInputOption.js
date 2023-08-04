import classNames from 'classnames';
import PropTypes from 'prop-types';
import React from 'react';
import EnhancedSelectInputOption from './EnhancedSelectInputOption';
import styles from './HintedSelectInputOption.css';

function HintedSelectInputOption(props) {
  const {
    id,
    value,
    hint,
    depth,
    isSelected,
    isDisabled,
    isMultiSelect,
    isMobile,
    ...otherProps
  } = props;

  return (
    <EnhancedSelectInputOption
      id={id}
      depth={depth}
      isSelected={isSelected}
      isDisabled={isDisabled}
      isHidden={isDisabled}
      isMultiSelect={isMultiSelect}
      isMobile={isMobile}
      {...otherProps}
    >
      <div className={classNames(
        styles.optionText,
        isMobile && styles.isMobile
      )}
      >
        <div>{typeof value === 'function' ? value() : value}</div>

        {
          hint != null &&
            <div className={styles.hintText} title={hint}>
              {hint}
            </div>
        }
      </div>
    </EnhancedSelectInputOption>
  );
}

HintedSelectInputOption.propTypes = {
  id: PropTypes.oneOfType([PropTypes.string, PropTypes.number]).isRequired,
  value: PropTypes.oneOfType([PropTypes.string, PropTypes.func]).isRequired,
  hint: PropTypes.node,
  depth: PropTypes.number,
  isSelected: PropTypes.bool.isRequired,
  isDisabled: PropTypes.bool.isRequired,
  isMultiSelect: PropTypes.bool.isRequired,
  isMobile: PropTypes.bool.isRequired
};

HintedSelectInputOption.defaultProps = {
  isDisabled: false,
  isHidden: false,
  isMultiSelect: false
};

export default HintedSelectInputOption;
