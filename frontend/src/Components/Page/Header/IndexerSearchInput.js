import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Autosuggest from 'react-autosuggest';
import Icon from 'Components/Icon';
import keyboardShortcuts, { shortcuts } from 'Components/keyboardShortcuts';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import IndexerSearchResult from './IndexerSearchResult';
import styles from './IndexerSearchInput.css';

const ADD_NEW_TYPE = 'addNew';

class IndexerSearchInput extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._autosuggest = null;

    this.state = {
      value: '',
      suggestions: []
    };
  }

  componentDidMount() {
    this.props.bindShortcut(shortcuts.MOVIE_SEARCH_INPUT.key, this.focusInput);
  }

  //
  // Control

  setAutosuggestRef = (ref) => {
    this._autosuggest = ref;
  };

  focusInput = (event) => {
    event.preventDefault();
    this._autosuggest.input.focus();
  };

  getSectionSuggestions(section) {
    return section.suggestions;
  }

  renderSectionTitle(section) {
    return (
      <div className={styles.sectionTitle}>
        {section.title}

        {
          section.loading &&
            <LoadingIndicator
              className={styles.loading}
              rippleClassName={styles.ripple}
              size={20}
            />
        }
      </div>
    );
  }

  getSuggestionValue({ title }) {
    return title;
  }

  renderSuggestion(item, { query }) {
    if (item.type === ADD_NEW_TYPE) {
      return (
        <div className={styles.addNewMovieSuggestion}>
          Search for {query}
        </div>
      );
    }

    return (
      <IndexerSearchResult
        {...item.item}
        match={item.matches[0]}
      />
    );
  }

  reset() {
    this.setState({
      value: '',
      suggestions: [],
      loading: false
    });
  }

  //
  // Listeners

  onChange = (event, { newValue, method }) => {
    if (method === 'up' || method === 'down') {
      return;
    }

    this.setState({ value: newValue });
  };

  onKeyDown = (event) => {
    if (event.shiftKey || event.altKey || event.ctrlKey) {
      return;
    }

    if (event.key === 'Escape') {
      this.reset();
      return;
    }

    if (event.key !== 'Tab' && event.key !== 'Enter') {
      return;
    }

    const {
      suggestions,
      value
    } = this.state;

    const {
      highlightedSectionIndex
    } = this._autosuggest.state;

    if (!suggestions.length || highlightedSectionIndex) {
      this.props.onGoToAddNewMovie(value);
      this._autosuggest.input.blur();
      this.reset();

      return;
    }

    this._autosuggest.input.blur();
    this.reset();
  };

  onBlur = () => {
    this.reset();
  };

  onSuggestionsClearRequested = () => {
    this.setState({
      suggestions: [],
      loading: false
    });
  };

  onSuggestionsFetchRequested = () => {
    this.setState({
      suggestions: [],
      loading: false
    });
  };

  onSuggestionSelected = (event, { suggestion }) => {
    if (suggestion.type === ADD_NEW_TYPE) {
      this.props.onGoToAddNewMovie(this.state.value);
    }
  };

  //
  // Render

  render() {
    const {
      value
    } = this.state;

    const suggestionGroups = [];

    suggestionGroups.push({
      title: translate('SearchIndexers'),
      suggestions: [
        {
          type: ADD_NEW_TYPE,
          title: value
        }
      ]
    });

    const inputProps = {
      ref: this.setInputRef,
      className: styles.input,
      name: 'movieSearch',
      value,
      placeholder: translate('Search'),
      autoComplete: 'off',
      spellCheck: false,
      onChange: this.onChange,
      onKeyDown: this.onKeyDown,
      onBlur: this.onBlur,
      onFocus: this.onFocus
    };

    const theme = {
      container: styles.container,
      containerOpen: styles.containerOpen,
      suggestionsContainer: styles.movieContainer,
      suggestionsList: styles.list,
      suggestion: styles.listItem,
      suggestionHighlighted: styles.highlighted
    };

    return (
      <div className={styles.wrapper}>
        <Icon name={icons.SEARCH} />

        <Autosuggest
          ref={this.setAutosuggestRef}
          id={name}
          inputProps={inputProps}
          theme={theme}
          focusInputOnSuggestionClick={false}
          multiSection={true}
          suggestions={suggestionGroups}
          getSectionSuggestions={this.getSectionSuggestions}
          renderSectionTitle={this.renderSectionTitle}
          getSuggestionValue={this.getSuggestionValue}
          renderSuggestion={this.renderSuggestion}
          onSuggestionSelected={this.onSuggestionSelected}
          onSuggestionsFetchRequested={this.onSuggestionsFetchRequested}
          onSuggestionsClearRequested={this.onSuggestionsClearRequested}
        />
      </div>
    );
  }
}

IndexerSearchInput.propTypes = {
  onGoToAddNewMovie: PropTypes.func.isRequired,
  bindShortcut: PropTypes.func.isRequired
};

export default keyboardShortcuts(IndexerSearchInput);
