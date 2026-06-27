import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Link from 'Components/Link/Link';
import styles from './Card.css';

class Card extends Component {

  //
  // Render

  render() {
    const {
      ariaLabel,
      className,
      overlayClassName,
      overlayContent,
      children,
      onPress,
      title
    } = this.props;

    if (overlayContent) {
      return (
        <div className={className}>
          <Link
            className={styles.underlay}
            aria-label={ariaLabel}
            onPress={onPress}
            title={title}
          />

          <div className={overlayClassName}>
            {children}
          </div>
        </div>
      );
    }

    return (
      <Link
        className={className}
        aria-label={ariaLabel}
        onPress={onPress}
        title={title}
      >
        {children}
      </Link>
    );
  }
}

Card.propTypes = {
  ariaLabel: PropTypes.string,
  className: PropTypes.string.isRequired,
  overlayClassName: PropTypes.string.isRequired,
  overlayContent: PropTypes.bool.isRequired,
  children: PropTypes.node.isRequired,
  onPress: PropTypes.func.isRequired,
  title: PropTypes.string
};

Card.defaultProps = {
  className: styles.card,
  overlayClassName: styles.overlay,
  overlayContent: false
};

export default Card;
