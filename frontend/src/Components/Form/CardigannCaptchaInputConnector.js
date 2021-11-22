import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { getCaptchaCookie, refreshCaptcha, resetCaptcha } from 'Store/Actions/captchaActions';
import CardigannCaptchaInput from './CardigannCaptchaInput';

function createMapStateToProps() {
  return createSelector(
    (state) => state.captcha,
    (captcha) => {
      return captcha;
    }
  );
}

const mapDispatchToProps = {
  refreshCaptcha,
  getCaptchaCookie,
  resetCaptcha
};

class CardigannCaptchaInputConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.onRefreshPress();
  }

  componentWillUnmount = () => {
    this.props.resetCaptcha();
  };

  //
  // Listeners

  onRefreshPress = () => {
    const {
      provider,
      providerData,
      name,
      onChange
    } = this.props;

    onChange({ name, value: '' });
    this.props.resetCaptcha();
    this.props.refreshCaptcha({ provider, providerData });

  };

  //
  // Render

  render() {
    return (
      <CardigannCaptchaInput
        {...this.props}
        onRefreshPress={this.onRefreshPress}
      />
    );
  }
}

CardigannCaptchaInputConnector.propTypes = {
  provider: PropTypes.string.isRequired,
  providerData: PropTypes.object.isRequired,
  name: PropTypes.string.isRequired,
  token: PropTypes.string,
  onChange: PropTypes.func.isRequired,
  refreshCaptcha: PropTypes.func.isRequired,
  getCaptchaCookie: PropTypes.func.isRequired,
  resetCaptcha: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(CardigannCaptchaInputConnector);
