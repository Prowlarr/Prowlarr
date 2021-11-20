import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons, sizes } from 'Helpers/Props';
import styles from './QueryParameterOption.css';

class QueryParameterOption extends Component {

  //
  // Listeners

  onPress = () => {
    const {
      token,
      tokenSeparator,
      isFullFilename,
      onPress
    } = this.props;

    let tokenValue = token;

    tokenValue = tokenValue.replace(/ /g, tokenSeparator);

    onPress({ isFullFilename, tokenValue });
  }

  //
  // Render
  render() {
    const {
      token,
      tokenSeparator,
      example,
      footNote,
      isFullFilename,
      size
    } = this.props;

    return (
      <Link
        className={classNames(
          styles.option,
          styles[size],
          isFullFilename && styles.isFullFilename
        )}
        onPress={this.onPress}
      >
        <div className={styles.token}>
          {token.replace(/ /g, tokenSeparator)}
        </div>

        <div className={styles.example}>
          {example.replace(/ /g, tokenSeparator)}

          {
            footNote !== 0 &&
              <Icon className={styles.footNote} name={icons.FOOTNOTE} />
          }
        </div>
      </Link>
    );
  }
}

QueryParameterOption.propTypes = {
  token: PropTypes.string.isRequired,
  example: PropTypes.string.isRequired,
  footNote: PropTypes.number.isRequired,
  tokenSeparator: PropTypes.string.isRequired,
  isFullFilename: PropTypes.bool.isRequired,
  size: PropTypes.oneOf([sizes.SMALL, sizes.LARGE]),
  onPress: PropTypes.func.isRequired
};

QueryParameterOption.defaultProps = {
  footNote: 0,
  size: sizes.SMALL,
  isFullFilename: false
};

export default QueryParameterOption;
