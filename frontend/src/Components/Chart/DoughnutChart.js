import Chart from 'chart.js/auto';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { defaultFontFamily } from 'Styles/Variables/fonts';

function getColors(kind) {

  const style = getComputedStyle(document.body);
  return style.getPropertyValue('--chartColorsDiversified').split(',');
}

class DoughnutChart extends Component {
  constructor(props) {
    super(props);
    this.canvasRef = React.createRef();
  }

  componentDidMount() {
    this.myChart = new Chart(this.canvasRef.current, {
      type: 'doughnut',
      options: {
        maintainAspectRatio: false,
        plugins: {
          title: {
            display: true,
            align: 'start',
            text: this.props.title,
            padding: {
              bottom: 30
            },
            font: {
              size: 14,
              family: defaultFontFamily
            }
          },
          legend: {
            position: 'bottom'
          }
        }
      },
      data: {
        labels: this.props.data.map((d) => d.label),
        datasets: [{
          label: this.props.title,
          data: this.props.data.map((d) => d.value),
          backgroundColor: getColors()
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

DoughnutChart.propTypes = {
  data: PropTypes.arrayOf(PropTypes.object).isRequired,
  title: PropTypes.string.isRequired
};

DoughnutChart.defaultProps = {
  data: [],
  title: ''
};

export default DoughnutChart;
