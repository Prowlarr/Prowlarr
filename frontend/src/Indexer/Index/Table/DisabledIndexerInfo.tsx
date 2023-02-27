import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import formatDateTime from 'Utilities/Date/formatDateTime';
import translate from 'Utilities/String/translate';
import styles from './DisabledIndexerInfo.css';

interface DisabledIndexerInfoProps {
  mostRecentFailure: Date;
  disabledTill: Date;
  initialFailure: Date;
  longDateFormat: string;
  timeFormat: string;
}

function DisabledIndexerInfo(props: DisabledIndexerInfoProps) {
  const {
    mostRecentFailure,
    disabledTill,
    initialFailure,
    longDateFormat,
    timeFormat,
  } = props;

  return (
    <DescriptionList>
      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title={translate('InitialFailure')}
        data={formatDateTime(initialFailure, longDateFormat, timeFormat)}
      />

      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title={translate('LastFailure')}
        data={formatDateTime(mostRecentFailure, longDateFormat, timeFormat)}
      />

      <DescriptionListItem
        titleClassName={styles.title}
        descriptionClassName={styles.description}
        title={translate('DisabledUntil')}
        data={formatDateTime(disabledTill, longDateFormat, timeFormat)}
      />
    </DescriptionList>
  );
}

export default DisabledIndexerInfo;
