import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AppProfileSelectInputConnector from 'Components/Form/AppProfileSelectInputConnector';
import SelectInput from 'Components/Form/SelectInput';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContentFooter from 'Components/Page/PageContentFooter';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import DeleteIndexerModal from './Delete/DeleteIndexerModal';
import IndexerEditorFooterLabel from './IndexerEditorFooterLabel';
import TagsModal from './Tags/TagsModal';
import styles from './IndexerEditorFooter.css';

const NO_CHANGE = 'noChange';

class IndexerEditorFooter extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      enable: NO_CHANGE,
      appProfileId: NO_CHANGE,
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
        enable: NO_CHANGE,
        appProfileId: NO_CHANGE,
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
  };

  onApplyTagsPress = (tags, applyTags) => {
    this.setState({
      savingTags: true,
      isTagsModalOpen: false
    });

    this.props.onSaveSelected({
      tags,
      applyTags
    });
  };

  onDeleteSelectedPress = () => {
    this.setState({ isDeleteMovieModalOpen: true });
  };

  onDeleteMovieModalClose = () => {
    this.setState({ isDeleteMovieModalOpen: false });
  };

  onTagsPress = () => {
    this.setState({ isTagsModalOpen: true });
  };

  onTagsModalClose = () => {
    this.setState({ isTagsModalOpen: false });
  };

  //
  // Render

  render() {
    const {
      indexerIds,
      selectedCount,
      isSaving,
      isDeleting
    } = this.props;

    const {
      enable,
      appProfileId,
      savingTags,
      isTagsModalOpen,
      isDeleteMovieModalOpen
    } = this.state;

    const enableOptions = [
      { key: NO_CHANGE, value: translate('NoChange'), disabled: true },
      { key: 'true', value: translate('Enabled') },
      { key: 'false', value: translate('Disabled') }
    ];

    return (
      <PageContentFooter>
        <div className={styles.inputContainer}>
          <IndexerEditorFooterLabel
            label={translate('EnableIndexer')}
            isSaving={isSaving && enable !== NO_CHANGE}
          />

          <SelectInput
            name="enable"
            value={enable}
            values={enableOptions}
            isDisabled={!selectedCount}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.inputContainer}>
          <IndexerEditorFooterLabel
            label={translate('SyncProfile')}
            isSaving={isSaving && appProfileId !== NO_CHANGE}
          />

          <AppProfileSelectInputConnector
            name="appProfileId"
            value={appProfileId}
            includeNoChange={true}
            isDisabled={!selectedCount}
            onChange={this.onInputChange}
          />
        </div>

        <div className={styles.buttonContainer}>
          <div className={styles.buttonContainerContent}>
            <IndexerEditorFooterLabel
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
          indexerIds={indexerIds}
          onApplyTagsPress={this.onApplyTagsPress}
          onModalClose={this.onTagsModalClose}
        />

        <DeleteIndexerModal
          isOpen={isDeleteMovieModalOpen}
          indexerIds={indexerIds}
          onModalClose={this.onDeleteMovieModalClose}
        />
      </PageContentFooter>
    );
  }
}

IndexerEditorFooter.propTypes = {
  indexerIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  selectedCount: PropTypes.number.isRequired,
  isSaving: PropTypes.bool.isRequired,
  saveError: PropTypes.object,
  isDeleting: PropTypes.bool.isRequired,
  deleteError: PropTypes.object,
  onSaveSelected: PropTypes.func.isRequired
};

export default IndexerEditorFooter;
