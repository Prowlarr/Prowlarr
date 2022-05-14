import Chart from 'chart.js/auto';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import colors from 'Styles/Variables/colors';

class StackedBarChart extends Component {
  constructor(props) {
    super(props);
    this.canvasRef = React.createRef();
  }

  componentDidMount() {
    this.myChart = new Chart(this.canvasRef.current, {
      type: 'bar',
      options: {
        maintainAspectRatio: false,
        scales: {
          x: {
            stacked: true,
            ticks: {
              stepSize: this.props.stepSize
            }
          },
          y: {
            stacked: true,
            ticks: {
              stepSize: this.props.stepSize
            }
          }
        },
        plugins: {
          title: {
            display: true,
            text: this.props.title
          }
        }
      },
      data: {
        labels: this.props.data.labels,
        datasets: this.props.data.datasets.map((d, index) => {
          return {
            label: d.label,
            data: d.data,
            backgroundColor: colors.chartColorsDiversified[index]
          };
        })
      }
    });
  }

  componentDidUpdate() {
    this.myChart.data.labels = this.props.data.labels;
    this.myChart.data.datasets = this.props.data.datasets.map((d, index) => {
      return {
        label: d.label,
        data: d.data,
        backgroundColor: colors.chartColorsDiversified[index]
      };
    });
    this.myChart.update();
  }

  render() {
    return (
      <canvas ref={this.canvasRef} />
    );
  }
}

StackedBarChart.propTypes = {
  data: PropTypes.object.isRequired,
  title: PropTypes.string.isRequired,
  stepSize: PropTypes.number
};

StackedBarChart.defaultProps = {
  title: '',
  stepSize: 1
};

export default StackedBarChart;
