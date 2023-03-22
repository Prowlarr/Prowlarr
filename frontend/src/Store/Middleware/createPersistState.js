import { get, merge as _merge, reduce, set } from 'lodash-es';
import persistState from 'redux-localstorage';
import actions from 'Store/Actions';
import migrate from 'Store/Migrators/migrate';

const columnPaths = [];

const paths = reduce([...actions], (acc, action) => {
  if (action.persistState) {
    action.persistState.forEach((path) => {
      if (path.match(/\.columns$/)) {
        columnPaths.push(path);
      }

      acc.push(path);
    });
  }

  return acc;
}, []);

function mergeColumns(path, initialState, persistedState, computedState) {
  const initialColumns = get(initialState, path);
  const persistedColumns = get(persistedState, path);

  if (!persistedColumns || !persistedColumns.length) {
    return;
  }

  const columns = [];

  // Add persisted columns in the same order they're currently in
  // as long as they haven't been removed.

  persistedColumns.forEach((persistedColumn) => {
    const column = initialColumns.find((i) => i.name === persistedColumn.name);

    if (column) {
      columns.push({
        ...column,
        isVisible: persistedColumn.isVisible
      });
    }
  });

  // Add any columns added to the app in the initial position.
  initialColumns.forEach((initialColumn, index) => {
    const persistedColumnIndex = persistedColumns.findIndex((i) => i.name === initialColumn.name);
    const column = Object.assign({}, initialColumn);

    if (persistedColumnIndex === -1) {
      columns.splice(index, 0, column);
    }
  });

  // Set the columns in the persisted state
  set(computedState, path, columns);
}

function slicer(pathList) {
  return (state) => {
    const subset = {};

    pathList.forEach((path) => {
      set(subset, path, get(state, path));
    });

    return subset;
  };
}

function serialize(obj) {
  return JSON.stringify(obj, null, 2);
}

function merge(initialState, persistedState) {
  if (!persistedState) {
    return initialState;
  }

  const computedState = {};

  _merge(computedState, initialState, persistedState);

  columnPaths.forEach((columnPath) => {
    mergeColumns(columnPath, initialState, persistedState, computedState);
  });

  return computedState;
}

const config = {
  slicer,
  serialize,
  merge,
  key: 'prowlarr'
};

export default function createPersistState() {
  // Migrate existing local storage before proceeding
  const persistedState = JSON.parse(localStorage.getItem(config.key));
  migrate(persistedState);
  localStorage.setItem(config.key, serialize(persistedState));

  return persistState(paths, config);
}
