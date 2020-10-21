import PropTypes from 'prop-types';
import React, { Component } from 'react';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import DeleteMovieModal from './Delete/DeleteMovieModal';
import MovieEditorFooterLabel from './MovieEditorFooterLabel';
import TagsModal from './Tags/TagsModal';
import styles from './MovieEditorFooter.css';

const NO_CHANGE = 'noChange';

class MovieEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      savingTags: false,
      isDeleteMovieModalOpen: false,
      isTagsModalOpen: false
    };
  }

  componentDidUpdate(prevProps) {
    const {
      isSaving,
      saveError
    } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
      this.setState({
        savingTags: false
      });
    }
  }

  //
  // Listeners

  onInputChange = ({ name, value }) => {
    this.setState({ [name]: value });

    if (value === NO_CHANGE) {
      return;
    }

    switch (name) {
      default:
        this.props.onSaveSelected({ [name]: value });
    }
  }

  onApplyTagsPress = (tags, applyTags) => {
    this.setState({
      savingTags: true,
      isTagsModalOpen: false
    });

    this.props.onSaveSelected({
      tags,
      applyTags
    });
  }

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteMovieModalOpen: true });
  }

  onDeleteMovieModalClose = () => {
    this.setState({ isDeleteMovieModalOpen: false });
  }

  onTagsPress = () => {
    this.setState({ isTagsModalOpen: true });
  }

  onTagsModalClose = () => {
    this.setState({ isTagsModalOpen: false });
  }

  //
  // Render

  render() {
    const {
      movieIds,
      selectedCount,
      isSaving,
      isDeleting
    } = this.props;

    const {
      savingTags,
      isTagsModalOpen,
      isDeleteMovieModalOpen
    } = this.state;

    return (
      <PageContentFooter>
        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <MovieEditorFooterLabel
              label={translate('IndexersSelectedInterp', [selectedCount])}
              isSaving={false}
            />

            <div className={styles.buttons}>
              <div>
                <SpinnerButton
                  className={styles.tagsButton}
                  isSpinning={isSaving && savingTags}
                  isDisabled={!selectedCount}
                  onPress={this.onTagsPress}
                >
                  {translate('SetTags')}
                </SpinnerButton>
              </div>

              <SpinnerButton
                className={styles.deleteSelectedButton}
                kind={kinds.DANGER}
                isSpinning={isDeleting}
                isDisabled={!selectedCount || isDeleting}
                onPress={this.onDeleteSelectedPress}
              >
                {translate('Delete')}
              </SpinnerButton>
            </div>
          </div>
        </div>

        <TagsModal
          isOpen={isTagsModalOpen}
          movieIds={movieIds}
          onApplyTagsPress={this.onApplyTagsPress}
          onModalClose={this.onTagsModalClose}
        />

        <DeleteMovieModal
          isOpen={isDeleteMovieModalOpen}
          movieIds={movieIds}
          onModalClose={this.onDeleteMovieModalClose}
        />
      </PageContentFooter>
    );
  }
}

MovieEditorFooter.propTypes = {
  movieIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSaveSelected: PropTypes.func.isRequired
};

export default MovieEditorFooter;
