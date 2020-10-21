import PropTypes from 'prop-types';
import React, { Component } from 'react';
import IndexersSelectInputConnector from 'Components/Form/IndexersSelectInputConnector';
import TextInput from 'Components/Form/TextInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import styles from './SearchFooter.css';

class SearchFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchingReleases: false,
      searchQuery: '',
      indexerIds: []
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
    this.props.onSearchPress(this.state.searchQuery, this.state.indexerIds);
  }

  //
  // Render

  render() {
    const {
      isFetching
    } = this.props;

    const {
      searchQuery,
      indexerIds
    } = this.state;

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <TextInput
            name='searchQuery'
            placeholder='Query'
            value={searchQuery}
            isDisabled={isFetching}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.indexerContainer}>
          <IndexersSelectInputConnector
            name='indexerIds'
            placeholder='Indexers'
            value={indexerIds}
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
                isDisabled={isFetching}
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
  searchError: PropTypes.object
};

export default SearchFooter;
