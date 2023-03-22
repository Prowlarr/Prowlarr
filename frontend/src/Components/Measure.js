import { debounce } from 'lodash-es';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ReactMeasure from 'react-measure';

class Measure extends Component {

  //
  // Lifecycle

  componentWillUnmount() {
    this.onMeasure.cancel();
  }

  //
  // Listeners

  onMeasure = debounce((payload) => {
    this.props.onMeasure(payload);
  }, 250, { leading: true, trailing: false });

  //
  // Render

  render() {
    return (
      <ReactMeasure
        {...this.props}
      />
    );
  }
}

Measure.propTypes = {
  onMeasure: PropTypes.func.isRequired
};

export default Measure;
