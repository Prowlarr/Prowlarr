import { createSelector } from 'reselect';

const selectTableOptions = createSelector(
  (state) => state.indexerIndex.tableOptions,
  (tableOptions) => tableOptions
);

export default selectTableOptions;
