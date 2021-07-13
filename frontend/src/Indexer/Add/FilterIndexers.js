import PropTypes from 'prop-types';
import React, { useEffect, useState } from 'react';
import SelectInput from '../../Components/Form/SelectInput';
import TableHeaderCell from '../../Components/Table/TableHeaderCell';
import translate from '../../Utilities/String/translate';
import styles from './FilterIndexers.css';

const defaultState = [
  {
    key: 'showAll',
    value: translate('ShowAll'),
    disabled: false
  }
];

function FilterIndexers({ indexers, setIndexers }) {
  const [languages, setLanguages] = useState([]);
  const [protocols, setProtocols] = useState([]);
  const [privacy, setPrivacy] = useState([]);
  const [languageValue, setLanguageValue] = useState('showAll');
  const [privacyValue, setPrivacyValue] = useState('showAll');
  const [protocolValue, setProtocolValue] = useState('showAll');

  /**
   * Filters all Indexers by a key
   * @param {String} filterParam
   * @returns {{disabled: boolean, value: string, key: string}[]}
   */
  function filterIndexersForPrepare(filterParam) {
    const uniqueLanguages = Array.from(new Set(indexers.map((index) => index[filterParam]))).sort();
    return defaultState.concat(uniqueLanguages.map((index) => ({
      key: index,
      value: index,
      disabled: false
    })));
  }

  useEffect(() => {
    if (indexers.length > 0) {
      setLanguages(filterIndexersForPrepare('language'));
      setProtocols(filterIndexersForPrepare('protocol'));
      setPrivacy(filterIndexersForPrepare('privacy'));
    }
  }, [indexers]);

  /**
   * Figures out what value to display in the drop down box &
   *  applies a filter if needed (isn't showAll)
   * @param {String} onChangeName
   * @param {String} testName
   * @param {Array} filteredIndexers
   * @param {String} onChangeValue
   * @param {String} stateValue
   * @returns {(*)[]}
   */
  function indexFilterWrapper(onChangeName, testName, filteredIndexers, onChangeValue, stateValue) {
    const value = onChangeName === testName ? onChangeValue : stateValue;
    if (value !== 'showAll') {
      filteredIndexers = filteredIndexers.filter((index) => index[testName] === value);
    }
    return [filteredIndexers, value];
  }

  function onChange({ name, value }) {
    let filteredIndexers = [...indexers];
    let tempValue = '';
    if (protocolValue !== 'showAll' || name === 'protocol') {
      [filteredIndexers, tempValue] = indexFilterWrapper(name, 'protocol', filteredIndexers, value, protocolValue);
      setProtocolValue(tempValue);
    }
    if (languageValue !== 'showAll' || name === 'language') {
      [filteredIndexers, tempValue] = indexFilterWrapper(name, 'language', filteredIndexers, value, languageValue);
      setLanguageValue(tempValue);
    }
    if (privacyValue !== 'showAll' || name === 'privacy') {
      [filteredIndexers, tempValue] = indexFilterWrapper(name, 'privacy', filteredIndexers, value, privacyValue);
      setPrivacyValue(tempValue);
    }
    setIndexers(filteredIndexers);
    if (!['language', 'privacy', 'protocol'].includes(name)) {
      console.warn(name, 'isn\'t supported');
    }
  }

  return (
    <tr className={styles.filterIndexers}>
      <TableHeaderCell name="protocol" className={styles.protocol}>
        <SelectInput
          name="protocol"
          onChange={onChange}
          value={protocolValue}
          values={protocols}
        />
      </TableHeaderCell>
      <th className={styles.name} />
      <TableHeaderCell name="language" className={styles.language}>
        <SelectInput
          name="language"
          onChange={onChange}
          value={languageValue}
          values={languages}
        />
      </TableHeaderCell>
      <TableHeaderCell name="privacy" className={styles.privacy}>
        <SelectInput
          name="privacy"
          onChange={onChange}
          value={privacyValue}
          values={privacy}
        />
      </TableHeaderCell>
    </tr>
  );
}

FilterIndexers.propTypes = {
  indexers: PropTypes.arrayOf(PropTypes.shape({
    language: PropTypes.string.isRequired,
    name: PropTypes.string.isRequired,
    privacy: PropTypes.oneOf(['private', 'public', 'semi-private'])
  })).isRequired,
  setIndexers: PropTypes.func.isRequired
};

export default FilterIndexers;
