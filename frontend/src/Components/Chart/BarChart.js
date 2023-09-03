import Chart from 'chart.js/auto';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { kinds } from 'Helpers/Props';
import { defaultFontFamily } from 'Styles/Variables/fonts';

function getColors(kind) {

  const style = getComputedStyle(document.body);

  if (kind === kinds.WARNING) {
    return style.getPropertyValue('--failedColors').split(',');
  }

  return style.getPropertyValue('--chartColors').split(',');
}

class BarChart extends Component {
  constructor(props) {
    super(props);
    this.canvasRef = React.createRef();
  }

  componentDidMount() {
    this.myChart = new Chart(this.canvasRef.current, {
      type: 'bar',
      options: {
        x: {
          ticks: {
            stepSize: this.props.stepSize
          }
        },
        y: {
          ticks: {
            stepSize: this.props.stepSize
          }
        },
        indexAxis: this.props.horizontal ? 'y' : 'x',
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
            display: this.props.legend
          }
        }
      },
      data: {
        labels: this.props.data.map((d) => d.label),
        datasets: [{
          label: this.props.title,
          data: this.props.data.map((d) => d.value),
          backgroundColor: getColors(this.props.kind)
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
  legend: PropTypes.bool,
  title: PropTypes.string.isRequired,
  kind: PropTypes.oneOf(kinds.all).isRequired,
  stepSize: PropTypes.number
};

BarChart.defaultProps = {
  data: [],
  horizontal: false,
  legend: false,
  title: '',
  kind: kinds.INFO,
  stepSize: 1
};

export default BarChart;
