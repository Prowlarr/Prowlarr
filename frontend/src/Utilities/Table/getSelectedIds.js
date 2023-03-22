import { reduce } from 'lodash-es';

function getSelectedIds(selectedState, { parseIds = true } = {}) {
  return reduce(selectedState, (result, value, id) => {
    if (value) {
      const parsedId = parseIds ? parseInt(id) : id;

      result.push(parsedId);
    }

    return result;
  }, []);
}

export default getSelectedIds;
