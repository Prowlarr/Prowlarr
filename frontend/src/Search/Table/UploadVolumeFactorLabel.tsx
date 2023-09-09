import React from 'react';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import { icons, kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

interface UploadVolumeFactorLabelProps {
  factor?: number;
}

function UploadVolumeFactorLabel({ factor }: UploadVolumeFactorLabelProps) {
  const value = Number(factor);

  if (isNaN(value) || value === 1.0) {
    return null;
  }

  if (value === 0.0) {
    return <Label kind={kinds.WARNING}>{translate('NoUpload')}</Label>;
  }

  return (
    <Label kind={kinds.INFO}>
      <Icon name={icons.CARET_UP} /> {(value * 100).toFixed(0)}%UL
    </Label>
  );
}

export default UploadVolumeFactorLabel;
