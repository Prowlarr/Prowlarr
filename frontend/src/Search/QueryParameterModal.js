import PropTypes from 'prop-types';
import React, { Component } from 'react';
import FieldSet from 'Components/FieldSet';
import SelectInput from 'Components/Form/SelectInput';
import TextInput from 'Components/Form/TextInput';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import translate from 'Utilities/String/translate';
import QueryParameterOption from './QueryParameterOption';
import styles from './QueryParameterModal.css';

const searchOptions = [
  { key: 'search', value: () => translate('BasicSearch') },
  { key: 'tvsearch', value: () => translate('TvSearch') },
  { key: 'movie', value: () => translate('MovieSearch') },
  { key: 'music', value: () => translate( 'AudioSearch') },
  { key: 'book', value: () => translate('BookSearch') }
];

const seriesTokens = [
  { token: '{ImdbId:tt1234567}', example: 'tt12345' },
  { token: '{TvdbId:12345}', example: '12345' },
  { token: '{TmdbId:12345}', example: '12345' },
  { token: '{TvMazeId:12345}', example: '54321' },
  { token: '{Season:00}', example: '01' },
  { token: '{Episode:00}', example: '01' }
];

const movieTokens = [
  { token: '{ImdbId:tt1234567}', example: 'tt12345' },
  { token: '{TmdbId:12345}', example: '12345' },
  { token: '{Year:2000}', example: '2005' }
];

const audioTokens = [
  { token: '{Artist:Some Body}', example: 'Nirvana' },
  { token: '{Album:Some Album}', example: 'Nevermind' },
  { token: '{Label:Some Label}', example: 'Geffen' }
];

const bookTokens = [
  { token: '{Author:Some Author}', example: 'J. R. R. Tolkien' },
  { token: '{Title:Some Book}', example: 'Lord of the Rings' }
];

class QueryParameterModal extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._selectionStart = null;
    this._selectionEnd = null;

    this.state = {
      separator: ' '
    };
  }

  //
  // Listeners

  onInputSelectionChange = (selectionStart, selectionEnd) => {
    this._selectionStart = selectionStart;
    this._selectionEnd = selectionEnd;
  };

  onOptionPress = ({ isFullFilename, tokenValue }) => {
    const {
      name,
      value,
      onSearchInputChange
    } = this.props;

    const selectionStart = this._selectionStart;
    const selectionEnd = this._selectionEnd;

    if (isFullFilename) {
      onSearchInputChange({ name, value: tokenValue });
    } else if (selectionStart == null) {
      onSearchInputChange({
        name,
        value: `${value}${tokenValue}`
      });
    } else {
      const start = value.substring(0, selectionStart);
      const end = value.substring(selectionEnd);
      const newValue = `${start}${tokenValue}${end}`;

      onSearchInputChange({ name, value: newValue });
      this._selectionStart = newValue.length;
      this._selectionEnd = newValue.length;
    }
  };

  onInputChange = ({ name, value }) => {
    this.props.onSearchInputChange({ value: '' });
    this.props.onInputChange({ name, value });
  };

  //
  // Render

  render() {
    const {
      name,
      value,
      searchType,
      isOpen,
      onSearchInputChange,
      onModalClose
    } = this.props;

    const {
      separator: tokenSeparator
    } = this.state;

    return (
      <Modal
        isOpen={isOpen}
        onModalClose={onModalClose}
      >
        <ModalContent onModalClose={onModalClose}>
          <ModalHeader>
            {translate('QueryOptions')}
          </ModalHeader>

          <ModalBody>
            <FieldSet legend={translate('SearchType')}>
              <div className={styles.groups}>
                <SelectInput
                  className={styles.namingSelect}
                  name="searchType"
                  value={searchType}
                  values={searchOptions}
                  onChange={this.onInputChange}
                />
              </div>
            </FieldSet>

            {
              searchType === 'tvsearch' &&
                <FieldSet legend={translate('TvSearch')}>
                  <div className={styles.groups}>
                    {
                      seriesTokens.map(({ token, example }) => {
                        return (
                          <QueryParameterOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            tokenSeparator={tokenSeparator}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

            {
              searchType === 'movie' &&
                <FieldSet legend={translate('MovieSearch')}>
                  <div className={styles.groups}>
                    {
                      movieTokens.map(({ token, example }) => {
                        return (
                          <QueryParameterOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            tokenSeparator={tokenSeparator}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

            {
              searchType === 'music' &&
                <FieldSet legend={translate('AudioSearch')}>
                  <div className={styles.groups}>
                    {
                      audioTokens.map(({ token, example }) => {
                        return (
                          <QueryParameterOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            tokenSeparator={tokenSeparator}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }

            {
              searchType === 'book' &&
                <FieldSet legend={translate('BookSearch')}>
                  <div className={styles.groups}>
                    {
                      bookTokens.map(({ token, example }) => {
                        return (
                          <QueryParameterOption
                            key={token}
                            name={name}
                            value={value}
                            token={token}
                            example={example}
                            tokenSeparator={tokenSeparator}
                            onPress={this.onOptionPress}
                          />
                        );
                      }
                      )
                    }
                  </div>
                </FieldSet>
            }
          </ModalBody>

          <ModalFooter>
            <TextInput
              name={name}
              value={value}
              onChange={onSearchInputChange}
              onSelectionChange={this.onInputSelectionChange}
            />
            <Button onPress={onModalClose}>
              {translate('Close')}
            </Button>
          </ModalFooter>
        </ModalContent>
      </Modal>
    );
  }
}

QueryParameterModal.propTypes = {
  name: PropTypes.string.isRequired,
  value: PropTypes.string.isRequired,
  searchType: PropTypes.string.isRequired,
  isOpen: PropTypes.bool.isRequired,
  onSearchInputChange: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default QueryParameterModal;
