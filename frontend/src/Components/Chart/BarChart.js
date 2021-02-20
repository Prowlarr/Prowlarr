import Chart from 'chart.js';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import colors from 'Styles/Variables/colors';

class BarChart extends Component {
  constructor(props) {
    super(props);
    this.canvasRef = React.createRef();
  }

  componentDidMount() {
    this.myChart = new Chart(this.canvasRef.current, {
      type: this.props.horizontal ? 'horizontalBar' : 'bar',
      options: {
        maintainAspectRatio: false
      },
      data: {
        labels: this.props.data.map((d) => d.label),
        datasets: [{
          label: this.props.title,
          data: this.props.data.map((d) => d.value),
          backgroundColor: colors.chartColors
        }]
      }
    });
  }

  componentDidUpdate() {
    this.myChart.data.labels = this.props.data.map((d) => d.label);
    this.myChart.data.datasets[0].data = this.props.data.map((d) => d.value);
    this.myChart.update();
  }

  render() {
    return (
      <canvas ref={this.canvasRef} />
    );
  }
}

BarChart.propTypes = {
  data: PropTypes.arrayOf(PropTypes.object).isRequired,
  horizontal: PropTypes.bool,
  title: PropTypes.string.isRequired
};

BarChart.defaultProps = {
  data: [],
  horizontal: false,
  title: ''
};

export default BarChart;
