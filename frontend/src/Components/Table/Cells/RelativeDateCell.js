import PropTypes from 'prop-types';
import React from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import TableRowCell from './TableRowCell';
import styles from './RelativeDateCell.css';

function createRelativeDateCellSelector() {
  return createSelector(createUISettingsSelector(), (uiSettings) => {
    return {
      showRelativeDates: uiSettings.showRelativeDates,
      shortDateFormat: uiSettings.shortDateFormat,
      longDateFormat: uiSettings.longDateFormat,
      timeFormat: uiSettings.timeFormat
    };
  });
}

function RelativeDateCell(props) {
  //
  // Render

  const {
    className,
    date,
    includeSeconds,
    component: Component,
    dispatch,
    ...otherProps
  } = props;

  const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
    useSelector(createRelativeDateCellSelector());

  if (!date) {
    return <Component className={className} {...otherProps} />;
  }

  return (
    <Component
      className={className}
      title={formatDateTime(date, longDateFormat, timeFormat, {
        includeSeconds,
        includeRelativeDay: !showRelativeDates
      })}
      {...otherProps}
    >
      {getRelativeDate(date, shortDateFormat, showRelativeDates, {
        timeFormat,
        includeSeconds,
        timeForToday: true
      })}
    </Component>
  );
}

RelativeDateCell.propTypes = {
  className: PropTypes.string.isRequired,
  date: PropTypes.string,
  includeSeconds: PropTypes.bool.isRequired,
  component: PropTypes.elementType,
  dispatch: PropTypes.func
};

RelativeDateCell.defaultProps = {
  className: styles.cell,
  includeSeconds: false,
  component: TableRowCell
};

export default RelativeDateCell;
