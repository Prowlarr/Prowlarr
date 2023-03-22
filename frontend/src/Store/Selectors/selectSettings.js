import { cloneDeep, filter, isEmpty, map, reduce, remove } from 'lodash-es';

function getValidationFailures(saveError) {
  if (!saveError || saveError.status !== 400) {
    return [];
  }

  return cloneDeep(saveError.responseJSON);
}

function mapFailure(failure) {
  return {
    message: failure.errorMessage,
    link: failure.infoLink,
    detailedMessage: failure.detailedDescription
  };
}

function selectSettings(item, pendingChanges, saveError) {
  const validationFailures = getValidationFailures(saveError);

  // Merge all settings from the item along with pending
  // changes to ensure any settings that were not included
  // with the item are included.
  const allSettings = Object.assign({}, item, pendingChanges);

  const settings = reduce(allSettings, (result, value, key) => {
    if (key === 'fields') {
      return result;
    }

    // Return a flattened value
    if (key === 'implementationName') {
      result.implementationName = item[key];

      return result;
    }

    if (key === 'definitionName') {
      result.definitionName = item[key];

      return result;
    }

    const setting = {
      value: item[key],
      errors: map(remove(validationFailures, (failure) => {
        return failure.propertyName.toLowerCase() === key.toLowerCase() && !failure.isWarning;
      }), mapFailure),

      warnings: map(remove(validationFailures, (failure) => {
        return failure.propertyName.toLowerCase() === key.toLowerCase() && failure.isWarning;
      }), mapFailure)
    };

    if (pendingChanges.hasOwnProperty(key)) {
      setting.previousValue = setting.value;
      setting.value = pendingChanges[key];
      setting.pending = true;
    }

    result[key] = setting;
    return result;
  }, {});

  const fields = reduce(item.fields, (result, f) => {
    const field = Object.assign({ pending: false }, f);
    const hasPendingFieldChange = pendingChanges.fields && pendingChanges.fields.hasOwnProperty(field.name);

    if (hasPendingFieldChange) {
      field.previousValue = field.value;
      field.value = pendingChanges.fields[field.name];
      field.pending = true;
    }

    field.errors = map(remove(validationFailures, (failure) => {
      return failure.propertyName.toLowerCase() === field.name.toLowerCase() && !failure.isWarning;
    }), mapFailure);

    field.warnings = map(remove(validationFailures, (failure) => {
      return failure.propertyName.toLowerCase() === field.name.toLowerCase() && failure.isWarning;
    }), mapFailure);

    result.push(field);
    return result;
  }, []);

  if (fields.length) {
    settings.fields = fields;
  }

  const validationErrors = filter(validationFailures, (failure) => {
    return !failure.isWarning;
  });

  const validationWarnings = filter(validationFailures, (failure) => {
    return failure.isWarning;
  });

  return {
    settings,
    validationErrors,
    validationWarnings,
    hasPendingChanges: !isEmpty(pendingChanges),
    hasSettings: !isEmpty(settings),
    pendingChanges
  };
}

export default selectSettings;
