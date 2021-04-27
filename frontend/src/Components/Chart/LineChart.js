import Chart from 'chart.js/auto';
import PropTypes from 'prop-types';
import React, { Component } from 'react';

class LineChart extends Component {
  constructor(props) {
    super(props);
    this.canvasRef = React.createRef();
  }

  componentDidMount() {
    this.myChart = new Chart(this.canvasRef.current, {
      type: 'line',
      options: {
        maintainAspectRatio: false
      },
      data: {
        labels: this.props.data.map((d) => d.time),
        datasets: [{
          label: this.props.title,
          data: this.props.data.map((d) => d.value),
          fill: 'none',
          pointRadius: 2,
          borderWidth: 1,
          lineTension: 0
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

LineChart.propTypes = {
  data: PropTypes.arrayOf(PropTypes.object).isRequired,
  title: PropTypes.string.isRequired
};

export default LineChart;
