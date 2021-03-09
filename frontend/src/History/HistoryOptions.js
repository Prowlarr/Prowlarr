import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';

class HistoryOptions extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      historyCleanupDays: props.historyCleanupDays
    };
  }

  componentDidUpdate(prevProps) {
    const {
      historyCleanupDays
    } = this.props;

    if (historyCleanupDays !== prevProps.historyCleanupDays) {
      this.setState({
        historyCleanupDays
      });
    }
  }

  //
  // Listeners

  onGlobalInputChange = ({ name, value }) => {
    const {
      dispatchSaveGeneralSettings
    } = this.props;

    const setting = { [name]: value };

    this.setState(setting, () => {
      dispatchSaveGeneralSettings(setting);
    });
  }

  //
  // Render

  render() {
    const {
      historyCleanupDays
    } = this.state;

    return (
      <Fragment>
        <FormGroup>
          <FormLabel>History Cleanup</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="historyCleanupDays"
            value={historyCleanupDays}
            helpText="Set to 0 to disable automatic cleanup"
            helpTextWarning="History items older than the selected number of days will be cleaned up automatically"
            onChange={this.onGlobalInputChange}
          />
        </FormGroup>
      </Fragment>
    );
  }
}

HistoryOptions.propTypes = {
  historyCleanupDays: PropTypes.bool.isRequired,
  dispatchSaveGeneralSettings: PropTypes.func.isRequired
};

export default HistoryOptions;
