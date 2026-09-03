import _ from 'lodash';

function getToggledRange(items, id, lastToggled, idProp = 'id') {
  const lastToggledIndex = _.findIndex(items, { [idProp]: lastToggled });
  const changedIndex = _.findIndex(items, { [idProp]: id });

  let lower = 0;
  let upper = 0;

  if (lastToggledIndex > changedIndex) {
    lower = changedIndex;
    upper = lastToggledIndex + 1;
  } else {
    lower = lastToggledIndex;
    upper = changedIndex;
  }

  return {
    lower,
    upper
  };
}

export default getToggledRange;
