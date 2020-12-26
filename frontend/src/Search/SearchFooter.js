import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IndexersSelectInputConnector from 'Components/Form/IndexersSelectInputConnector';
import NewznabCategorySelectInputConnector from 'Components/Form/NewznabCategorySelectInputConnector';
import TextInput from 'Components/Form/TextInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import SearchFooterLabel from './SearchFooterLabel';
import styles from './SearchFooter.css';

class SearchFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchingReleases: false,
      searchQuery: '',
      indexerIds: [],
      categories: []
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isFetching,
      searchError
    } = this.props;

    if (prevProps.isFetching && !isFetching && !searchError) {
      this.setState({
        searchingReleases: false
      });
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });
  }

  onSearchPress = () => {
    this.props.onSearchPress(this.state.searchQuery, this.state.indexerIds, this.state.categories);
  }

  //
  // Render

  render() {
    const {
      isFetching,
      hasIndexers
    } = this.props;

    const {
      searchQuery,
      indexerIds,
      categories
    } = this.state;

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <SearchFooterLabel
            label={'Query'}
            isSaving={false}
          />

          <TextInput
            name='searchQuery'
            value={searchQuery}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={'Indexers'}
            isSaving={false}
          />

          <IndexersSelectInputConnector
            name='indexerIds'
            value={indexerIds}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <SearchFooterLabel
            label={'Categories'}
            isSaving={false}
          />

          <NewznabCategorySelectInputConnector
            name='categories'
            value={categories}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <div className={styles.buttons}>

              <SpinnerButton
                className={styles.deleteSelectedButton}
                isSpinning={isFetching}
                isDisabled={isFetching || !hasIndexers}
                onPress={this.onSearchPress}
              >
                Search
              </SpinnerButton>
            </div>
          </div>
        </div>
      </PageContentFooter>
    );
  }
}

SearchFooter.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  hasIndexers: PropTypes.bool.isRequired,
  searchError: PropTypes.object
};

export default SearchFooter;
