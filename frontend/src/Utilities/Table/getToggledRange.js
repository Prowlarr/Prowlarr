import { findIndex } from 'lodash-es';

function getToggledRange(items, id, lastToggled) {
  const lastToggledIndex = findIndex(items, { id: lastToggled });
  const changedIndex = findIndex(items, { id });
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
