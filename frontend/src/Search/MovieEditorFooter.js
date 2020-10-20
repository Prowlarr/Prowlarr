import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextInput from 'Components/Form/TextInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import styles from './MovieEditorFooter.css';

class MovieEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchingReleases: false,
      searchQuery: ''
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
    this.props.onSearchPress(this.state.searchQuery);
  }

  //
  // Render

  render() {
    const {
      isFetching
    } = this.props;

    const {
      searchQuery
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

MovieEditorFooter.propTypes = {
  isFetching: PropTypes.bool.isRequired,
  onSearchPress: PropTypes.func.isRequired,
  searchError: PropTypes.object
};

export default MovieEditorFooter;
