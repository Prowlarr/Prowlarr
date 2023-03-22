import { reduce, uniq } from 'lodash-es';

function selectUniqueIds(items, idProp) {
  const ids = reduce(items, (result, item) => {
    if (item[idProp]) {
      result.push(item[idProp]);
    }

    return result;
  }, []);

  return uniq(ids);
}

export default selectUniqueIds;
