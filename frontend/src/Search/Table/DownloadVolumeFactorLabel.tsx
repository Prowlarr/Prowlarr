import React from 'react';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

interface DownloadVolumeFactorLabelProps {
  factor?: number;
}

function DownloadVolumeFactorLabel({ factor }: DownloadVolumeFactorLabelProps) {
  const value = Number(factor);

  if (isNaN(value) || value === 1.0) {
    return null;
  }

  if (value === 0.0) {
    return <Label kind={kinds.SUCCESS}>{translate('Freeleech')}</Label>;
  }

  return (
    <Label kind={value > 1.0 ? kinds.DANGER : kinds.PRIMARY}>
      <Icon name={icons.CARET_DOWN} /> {(value * 100).toFixed(0)}%
    </Label>
  );
}

export default DownloadVolumeFactorLabel;
