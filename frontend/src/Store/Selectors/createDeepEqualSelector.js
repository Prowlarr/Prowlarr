import { isEqual } from 'lodash-es';
import { createSelectorCreator, defaultMemoize } from 'reselect';

const createDeepEqualSelector = createSelectorCreator(
  defaultMemoize,
  isEqual
);

export default createDeepEqualSelector;
