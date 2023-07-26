import PropTypes from 'prop-types';
import React, { Component, Fragment } from 'react';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

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
  };

  //
  // Render

  render() {
    const {
      historyCleanupDays
    } = this.state;

    return (
      <Fragment>
        <FormGroup>
          <FormLabel>{translate('HistoryCleanup')}</FormLabel>

          <FormInputGroup
            type={inputTypes.NUMBER}
            name="historyCleanupDays"
            unit={translate('days')}
            value={historyCleanupDays}
            helpText={translate('HistoryCleanupDaysHelpText')}
            helpTextWarning={translate('HistoryCleanupDaysHelpTextWarning')}
            onChange={this.onGlobalInputChange}
          />
        </FormGroup>
      </Fragment>
    );
  }
}

HistoryOptions.propTypes = {
  historyCleanupDays: PropTypes.number.isRequired,
  dispatchSaveGeneralSettings: PropTypes.func.isRequired
};

export default HistoryOptions;
